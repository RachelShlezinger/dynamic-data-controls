"""
Abstract AI parser interface.

Any concrete parser (MockAI, real LLM, etc.) must implement this protocol so
AiProxyService in the .NET layer is never coupled to a specific provider.
"""
from __future__ import annotations

from abc import ABC, abstractmethod

from models.request import ParseRequest
from models.response import ParseResponse


class AiParser(ABC):
    """Base class for all AI parser implementations."""

    @abstractmethod
    def parse(self, request: ParseRequest) -> ParseResponse:
        """
        Translate a free-text description into a structured ParseResponse.

        Args:
            request: The incoming parse request containing description + context.

        Returns:
            A fully validated ParseResponse.

        Raises:
            ValueError: If the internal engine returns a response that cannot
                        be coerced into a valid ParseResponse.
        """
        ...
