"""
Application configuration loaded from environment variables.
All values have sensible defaults for local development.
"""
from __future__ import annotations

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Runtime configuration for the AI service."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        case_sensitive=False,
    )

    # Network
    host: str = "0.0.0.0"
    port: int = 8001

    # CORS — comma-separated list of allowed origins
    allowed_origins: str = "http://localhost:4200,http://localhost:5000"

    # AI engine selection: "mock" is the only supported value in Phase A
    ai_engine: str = "mock"

    @property
    def cors_origins(self) -> list[str]:
        """Return allowed origins as a list."""
        return [o.strip() for o in self.allowed_origins.split(",") if o.strip()]


# Module-level singleton — import this everywhere
settings = Settings()
