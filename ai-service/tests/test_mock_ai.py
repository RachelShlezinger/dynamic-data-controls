"""
Smoke tests for the MockAI engine.

Verifies that every keyword in MOCK_RESPONSES routes to the correct predefined
response, and that an unrecognised description falls back to DEFAULT.

Requirements: 7.4, 7.9
"""
import sys
import os

# Allow importing from the ai-service root when pytest is run from that directory
# or from the workspace root.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import pytest

from services.mock_ai import MOCK_RESPONSES, MockAiParser, find_response
from models.request import AiContext, ParseRequest


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_request(description: str) -> ParseRequest:
    """Build a minimal ParseRequest with the given description."""
    return ParseRequest(description=description, context=AiContext(entities=[]))


# ---------------------------------------------------------------------------
# find_response — keyword routing
# ---------------------------------------------------------------------------

class TestFindResponse:
    def test_keyword_katan_returns_minor_employees_response(self):
        result = find_response("עובד קטין עם שכר גבוה")
        assert result["controlName"] == MOCK_RESPONSES["קטין"]["controlName"]
        assert result["primaryEntity"] == MOCK_RESPONSES["קטין"]["primaryEntity"]
        assert result["conditions"] == MOCK_RESPONSES["קטין"]["conditions"]

    def test_keyword_taarich_sioom_returns_date_response(self):
        result = find_response("בדיקת תאריך_סיום")
        assert result["controlName"] == MOCK_RESPONSES["תאריך_סיום"]["controlName"]
        assert result["conditions"] == MOCK_RESPONSES["תאריך_סיום"]["conditions"]

    def test_keyword_machlaka_lo_peila_returns_inactive_dept_response(self):
        result = find_response("עובדים עם מחלקה_לא_פעילה")
        assert result["controlName"] == MOCK_RESPONSES["מחלקה_לא_פעילה"]["controlName"]
        assert result["primaryEntity"] == MOCK_RESPONSES["מחלקה_לא_פעילה"]["primaryEntity"]
        assert len(result["conditions"]) == 2
        assert result["conditions"][0]["fieldName"] == "is_active"
        assert result["conditions"][0]["operator"] == "Equal"
        assert result["conditions"][0]["value"] is True
        assert result["conditions"][1]["fieldName"] == "department_is_active"
        assert result["conditions"][1]["value"] is False

    def test_keyword_chaser_netunim_returns_missing_data_response(self):
        result = find_response("איתור חסר_נתונים במערכת")
        assert result["controlName"] == MOCK_RESPONSES["חסר_נתונים"]["controlName"]
        assert len(result["conditions"]) == 2
        assert all(c["operator"] == "IsNull" for c in result["conditions"])
        assert all(c["value"] is None for c in result["conditions"])

    def test_unknown_description_falls_back_to_default(self):
        result = find_response("תיאור שלא מכיל אף מילת מפתח מוכרת כלל")
        assert result == MOCK_RESPONSES["DEFAULT"]

    def test_default_key_itself_does_not_trigger_keyword_match(self):
        """'DEFAULT' as a substring should not match any non-DEFAULT keyword."""
        result = find_response("DEFAULT description")
        assert result == MOCK_RESPONSES["DEFAULT"]

    def test_first_matching_keyword_wins(self):
        """When a description contains both 'קטין' and 'תאריך_סיום', the first
        keyword that appears in MOCK_RESPONSES insertion order wins."""
        result = find_response("עובד קטין עם תאריך_סיום בעיתי")
        # "קטין" is inserted before "תאריך_סיום"
        assert result["controlName"] == MOCK_RESPONSES["קטין"]["controlName"]


# ---------------------------------------------------------------------------
# MockAiParser.parse — full pipeline smoke test
# ---------------------------------------------------------------------------

class TestMockAiParser:
    def setup_method(self):
        self.parser = MockAiParser()

    def test_parse_minor_employees(self):
        req = _make_request("עובד קטין עם שכר גבוה מאוד")
        response = self.parser.parse(req)
        assert response.controlName == "עובדים קטינים עם שכר גבוה"
        assert response.primaryEntity == "עובד"
        assert len(response.conditions) == 2
        operators = {c.operator for c in response.conditions}
        assert "LessThan" in operators
        assert "GreaterThan" in operators

    def test_parse_end_date(self):
        req = _make_request("בקרה על תאריך_סיום חוזה")
        response = self.parser.parse(req)
        assert response.controlName == "תאריך סיום מוקדם מתאריך התחלה"
        assert response.conditions[0].operator == "LessThan"

    def test_parse_inactive_department(self):
        req = _make_request("עובדים פעילים ב מחלקה_לא_פעילה")
        response = self.parser.parse(req)
        assert response.controlName == "עובדים פעילים במחלקה לא פעילה"
        assert len(response.conditions) == 2

    def test_parse_missing_data(self):
        req = _make_request("רשומות עם חסר_נתונים קריטיים")
        response = self.parser.parse(req)
        assert response.controlName == "רשומות עם נתונים חסרים"
        for cond in response.conditions:
            assert cond.operator == "IsNull"
            assert cond.value is None

    def test_parse_unknown_returns_default(self):
        req = _make_request("תיאור גנרי שאינו מכיל מילות מפתח")
        response = self.parser.parse(req)
        assert response.controlName == "בקרה לדוגמה"
        assert response.conditions[0].operator == "IsNotNull"

    def test_parse_returns_valid_pydantic_model(self):
        """Ensure parse() always returns a fully-validated ParseResponse."""
        from models.response import ParseResponse
        req = _make_request("עובד קטין")
        response = self.parser.parse(req)
        assert isinstance(response, ParseResponse)
        assert len(response.conditions) >= 1
