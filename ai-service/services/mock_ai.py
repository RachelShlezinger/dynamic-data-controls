"""
MockAI engine — returns predefined JSON responses keyed by Hebrew keywords.

Used in Phase A (development / testing) so the full AI pipeline can be
exercised without a live LLM.  Switching to a real provider only requires
replacing this module; nothing else changes.
"""
from __future__ import annotations

from models.request import ParseRequest
from models.response import ConditionItem, ParseResponse
from services.ai_parser import AiParser

# ---------------------------------------------------------------------------
# Predefined response dictionary
# Key   = substring to search for inside the incoming description (Hebrew).
# Value = raw dict that maps 1-to-1 to the ParseResponse model fields.
# ---------------------------------------------------------------------------
MOCK_RESPONSES: dict[str, dict] = {
    "קטין": {
        "controlName": "עובדים קטינים עם שכר גבוה",
        "primaryEntity": "עובד",
        "conditions": [
            {"fieldName": "גיל", "operator": "LessThan", "value": 18},
            {"fieldName": "שכר_חודשי", "operator": "GreaterThan", "value": 10000},
        ],
    },
    "תאריך_סיום": {
        "controlName": "תאריך סיום מוקדם מתאריך התחלה",
        "primaryEntity": "עובד",
        "conditions": [
            {
                "fieldName": "תאריך_סיום",
                "operator": "LessThan",
                "value": "{{תאריך_התחלה}}",
            }
        ],
    },
    "מחלקה_לא_פעילה": {
        "controlName": "עובדים פעילים במחלקה לא פעילה",
        "primaryEntity": "עובד",
        "conditions": [
            {"fieldName": "is_active", "operator": "Equal", "value": True},
            {"fieldName": "department_is_active", "operator": "Equal", "value": False},
        ],
    },
    "חסר_נתונים": {
        "controlName": "רשומות עם נתונים חסרים",
        "primaryEntity": "עובד",
        "conditions": [
            {"fieldName": "employee_name", "operator": "IsNull", "value": None},
            {"fieldName": "employee_salary", "operator": "IsNull", "value": None},
        ],
    },
    "DEFAULT": {
        "controlName": "בקרה לדוגמה",
        "primaryEntity": "עובד",
        "conditions": [
            {"fieldName": "גיל", "operator": "IsNotNull", "value": None}
        ],
    },
}


def find_response(description: str) -> dict:
    """
    Look up the best-matching predefined response for the given description.

    Iterates over non-DEFAULT keys in insertion order; returns the first entry
    whose key appears as a substring in *description*.  Falls back to DEFAULT
    when no keyword matches.

    Args:
        description: Free-text Hebrew description from the user.

    Returns:
        A raw dict matching the ParseResponse schema.
    """
    for keyword, response in MOCK_RESPONSES.items():
        if keyword == "DEFAULT":
            continue
        if keyword in description:
            return response

    return MOCK_RESPONSES["DEFAULT"]


class MockAiParser(AiParser):
    """Concrete AiParser that delegates to the MOCK_RESPONSES dictionary."""

    def parse(self, request: ParseRequest) -> ParseResponse:
        raw = find_response(request.description)

        conditions = [
            ConditionItem(
                fieldName=c["fieldName"],
                operator=c["operator"],
                value=c.get("value"),
            )
            for c in raw["conditions"]
        ]

        return ParseResponse(
            controlName=raw["controlName"],
            primaryEntity=raw["primaryEntity"],
            conditions=conditions,
        )
