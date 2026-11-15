"""
/parse router — exposes POST /parse for the .NET AiProxyService.
"""
from __future__ import annotations

import logging

from fastapi import APIRouter
from fastapi.responses import JSONResponse
from pydantic import ValidationError as PydanticValidationError

from models.request import ParseRequest
from models.response import ParseErrorResponse, ParseResponse
from services.mock_ai import MockAiParser

logger = logging.getLogger(__name__)

router = APIRouter()

# Use MockAI in Phase A; swap this for a real parser without touching the router.
_parser = MockAiParser()


@router.post(
    "/parse",
    response_model=ParseResponse,
    responses={
        422: {"model": ParseErrorResponse, "description": "AI schema validation error"},
        503: {"description": "AI engine unavailable"},
    },
    summary="Parse a free-text Hebrew description into a ControlDefinition",
)
async def parse_description(request: ParseRequest) -> ParseResponse:
    """
    Accepts a free-text description (10–1 000 chars) and optional metadata
    context, then returns a structured `ParseResponse` ready for the user to
    review and confirm before saving as a control.

    - **200** – Successful parse; body is a `ParseResponse`.
    - **422** – The AI engine returned a response that does not match the
      expected schema; body includes `missingFields`.
    - **503** – The AI engine raised an unexpected internal error.
    """
    try:
        result = _parser.parse(request)
        return result

    except (ValueError, PydanticValidationError) as exc:
        # The AI engine returned a payload that does not conform to ParseResponse.
        # Requirement 7.8: return 422 with the list of failing field names.
        logger.warning("AI schema validation error: %s", exc)

        if isinstance(exc, PydanticValidationError):
            # Extract the dotted field paths reported by Pydantic.
            missing = [
                ".".join(str(loc) for loc in e["loc"])
                for e in exc.errors()
            ]
            error_msg = "AI response does not match the expected schema"
        else:
            # ValueError raised explicitly by an AiParser implementation.
            missing: list[str] = []
            error_msg = str(exc) or "AI response does not match the expected schema"

        return JSONResponse(
            status_code=422,
            content=ParseErrorResponse(
                error=error_msg,
                missingFields=missing,
            ).model_dump(),
        )

    except Exception as exc:  # noqa: BLE001
        logger.exception("AI engine error during parse: %s", exc)
        return JSONResponse(
            status_code=503,
            content={"error": "AI service unavailable"},
        )
