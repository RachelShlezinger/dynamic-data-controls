"""
Integration tests for POST /parse endpoint.

Uses FastAPI TestClient (backed by httpx) to exercise the full router
pipeline including request validation, MockAI dispatch, and AI schema
error handling.

Requirements: 7.2, 7.3, 7.8
"""
from __future__ import annotations

import sys
import os

# Allow importing from the ai-service root when pytest is run from that
# directory or from the workspace root.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import pytest
from fastapi.testclient import TestClient

from main import app

client = TestClient(app)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _payload(description: str) -> dict:
    """Build a minimal valid /parse request body with the given description."""
    return {"description": description, "context": {"entities": []}}


# ---------------------------------------------------------------------------
# 200 — Successful parse
# ---------------------------------------------------------------------------

class TestParseSuccess:
    def test_description_with_katan_returns_200_and_correct_control_name(self):
        """POST with description containing 'קטין' → 200 with expected controlName."""
        resp = client.post("/parse", json=_payload("עובד קטין עם שכר גבוה מאוד"))
        assert resp.status_code == 200
        body = resp.json()
        assert body["controlName"] == "עובדים קטינים עם שכר גבוה"

    def test_valid_response_contains_all_required_fields(self):
        """Successful response body must have controlName, primaryEntity, conditions."""
        resp = client.post("/parse", json=_payload("תיאור כלשהו שמספיק ארוך לבדיקה"))
        assert resp.status_code == 200
        body = resp.json()
        assert "controlName" in body
        assert "primaryEntity" in body
        assert "conditions" in body
        assert isinstance(body["conditions"], list)
        assert len(body["conditions"]) >= 1

    def test_conditions_items_contain_required_fields(self):
        """Each condition must have fieldName, operator; value may be None."""
        resp = client.post("/parse", json=_payload("עובד קטין עם שכר גבוה"))
        assert resp.status_code == 200
        for cond in resp.json()["conditions"]:
            assert "fieldName" in cond
            assert "operator" in cond
            assert "value" in cond

    def test_unknown_description_returns_default_response(self):
        """A description matching no keyword returns the DEFAULT mock response."""
        resp = client.post("/parse", json=_payload("תיאור שאינו מכיל אף מילת מפתח מוכרת"))
        assert resp.status_code == 200
        assert resp.json()["controlName"] == "בקרה לדוגמה"


# ---------------------------------------------------------------------------
# 422 — Pydantic request-level validation (FastAPI/Pydantic, not our logic)
# ---------------------------------------------------------------------------

class TestParseRequestValidation:
    def test_description_shorter_than_10_chars_returns_422(self):
        """description < 10 chars is rejected by Pydantic min_length constraint."""
        resp = client.post("/parse", json=_payload("קצר"))
        assert resp.status_code == 422

    def test_empty_description_returns_422(self):
        """Empty description string is rejected before reaching the router logic."""
        resp = client.post("/parse", json=_payload(""))
        assert resp.status_code == 422

    def test_missing_description_field_returns_422(self):
        """A body with no description field at all must fail Pydantic validation."""
        resp = client.post("/parse", json={"context": {"entities": []}})
        assert resp.status_code == 422

    def test_missing_context_field_returns_422(self):
        """A body with no context field must fail Pydantic validation."""
        resp = client.post("/parse", json={"description": "תיאור תקין שמספיק ארוך"})
        assert resp.status_code == 422


# ---------------------------------------------------------------------------
# 422 — AI schema validation error (ValueError raised by the parser)
# ---------------------------------------------------------------------------

class TestParseAiSchemaError:
    def test_parser_raising_value_error_returns_422(self, monkeypatch):
        """When AiParser.parse() raises ValueError the router returns 422 ParseErrorResponse."""
        import routers.parse as parse_module

        class _BrokenParser:
            def parse(self, request):  # noqa: D102
                raise ValueError("controlName is missing from AI response")

        monkeypatch.setattr(parse_module, "_parser", _BrokenParser())

        resp = client.post("/parse", json=_payload("תיאור תקין שאמור להגיע לפארסר"))
        assert resp.status_code == 422
        body = resp.json()
        assert "error" in body
        assert "missingFields" in body
        assert isinstance(body["missingFields"], list)

    def test_parser_raising_value_error_error_message_is_propagated(self, monkeypatch):
        """The error message from the ValueError is surfaced in the 422 body."""
        import routers.parse as parse_module

        class _BrokenParser:
            def parse(self, request):  # noqa: D102
                raise ValueError("controlName is missing from AI response")

        monkeypatch.setattr(parse_module, "_parser", _BrokenParser())

        resp = client.post("/parse", json=_payload("תיאור תקין שאמור להגיע לפארסר"))
        body = resp.json()
        assert "controlName" in body["error"]


# ---------------------------------------------------------------------------
# 503 — AI engine unavailable
# ---------------------------------------------------------------------------

class TestParseServiceUnavailable:
    def test_parser_raising_runtime_error_returns_503(self, monkeypatch):
        """An unexpected exception from the parser results in 503."""
        import routers.parse as parse_module

        class _CrashingParser:
            def parse(self, request):  # noqa: D102
                raise RuntimeError("unexpected internal failure")

        monkeypatch.setattr(parse_module, "_parser", _CrashingParser())

        resp = client.post("/parse", json=_payload("תיאור תקין שאמור להגיע לפארסר"))
        assert resp.status_code == 503
        assert resp.json()["error"] == "AI service unavailable"
