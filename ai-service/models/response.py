"""
Pydantic v2 response models for the AI parse endpoint.
"""
from __future__ import annotations

from typing import Any

from pydantic import BaseModel, Field


class ConditionItem(BaseModel):
    """A single condition within a control definition returned by the AI."""

    fieldName: str = Field(..., description="Business-friendly field name")
    operator: str = Field(
        ...,
        description=(
            "Comparison operator — one of: Equal, NotEqual, GreaterThan, "
            "LessThan, GreaterThanOrEqual, LessThanOrEqual, "
            "Contains, StartsWith, IsNull, IsNotNull"
        ),
    )
    value: Any = Field(
        default=None,
        description="Comparison value; None for IsNull / IsNotNull operators",
    )


class ParseResponse(BaseModel):
    """Successful AI parse result returned as 200 OK."""

    controlName: str = Field(..., description="Suggested name for the new control")
    primaryEntity: str = Field(
        ..., description="Primary business entity targeted by the control"
    )
    conditions: list[ConditionItem] = Field(
        ...,
        min_length=1,
        description="One or more conditions that define the control rule",
    )


class ParseErrorResponse(BaseModel):
    """Error payload returned when AI response does not match the expected schema."""

    error: str = Field(..., description="Human-readable error message")
    missingFields: list[str] = Field(
        default_factory=list,
        description="Names of fields that were absent or invalid in the AI response",
    )
