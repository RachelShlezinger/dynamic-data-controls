"""
Pydantic v2 request models for the AI parse endpoint.
"""
from __future__ import annotations

from pydantic import BaseModel, Field


class FieldInfo(BaseModel):
    """Represents a single business field within an entity."""

    name: str = Field(..., description="Business-friendly field name")
    dataType: str = Field(
        ...,
        description="Field data type: Number | String | Date | Boolean",
    )
    description: str | None = Field(
        default=None, description="Optional human-readable description"
    )


class EntityInfo(BaseModel):
    """Represents a business entity with its available fields."""

    name: str = Field(..., description="Business-friendly entity name")
    fields: list[FieldInfo] = Field(
        default_factory=list,
        description="All fields belonging to this entity",
    )


class AiContext(BaseModel):
    """Context sent to the AI so it can reference valid entities and fields."""

    entities: list[EntityInfo] = Field(
        default_factory=list,
        description="All available business entities with their fields",
    )


class ParseRequest(BaseModel):
    """Request body for POST /parse."""

    description: str = Field(
        ...,
        min_length=10,
        max_length=1000,
        description="Free-text Hebrew description of the desired control rule",
    )
    context: AiContext = Field(
        ...,
        description="Metadata context (entities + fields) provided to the AI",
    )
