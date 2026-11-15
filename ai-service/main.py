"""
AI Service entry point — FastAPI application with CORS middleware.

Run locally:
    uvicorn main:app --reload --host 0.0.0.0 --port 8001
"""
from __future__ import annotations

import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from config import settings
from routers.parse import router as parse_router

# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(name)s — %(message)s",
)
logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# FastAPI app
# ---------------------------------------------------------------------------
app = FastAPI(
    title="Dynamic Data Controls — AI Service",
    description=(
        "Python FastAPI micro-service that translates free-text Hebrew descriptions "
        "into structured ControlDefinition JSON.  "
        "Phase A uses MockAI; Phase B will plug in a real LLM."
    ),
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
)

# ---------------------------------------------------------------------------
# CORS — allow the .NET backend (and Angular dev server) to call this service
# ---------------------------------------------------------------------------
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins,
    allow_credentials=True,
    allow_methods=["POST", "GET", "OPTIONS"],
    allow_headers=["*"],
)

# ---------------------------------------------------------------------------
# Routers
# ---------------------------------------------------------------------------
app.include_router(parse_router, tags=["AI Parse"])


# ---------------------------------------------------------------------------
# Health-check
# ---------------------------------------------------------------------------
@app.get("/health", tags=["Health"])
async def health() -> dict:
    """Returns 200 OK when the service is running."""
    return {"status": "ok", "engine": settings.ai_engine}
