import os
from dataclasses import dataclass, field
from pathlib import Path

from dotenv import load_dotenv

ROOT = Path(__file__).resolve().parents[1]


@dataclass(frozen=True)
class Settings:
    api_key: str = field(default="", repr=False)
    model: str = "gpt-4o-mini"
    emotion_mode: str = "auto"
    timeout_seconds: float = 20.0
    max_audio_bytes: int = 20 * 1024 * 1024
    max_audio_seconds: float = 120.0

    def __post_init__(self):
        if self.emotion_mode not in {"auto", "demo", "openai"}:
            raise ValueError("EMOTION_MODE must be auto, demo or openai")
        if self.emotion_mode == "openai" and not self.api_key:
            raise ValueError("OPENAI_API_KEY is required for openai mode")
        if self.max_audio_bytes <= 0 or self.max_audio_seconds <= 0 or self.timeout_seconds <= 0:
            raise ValueError("Audio limits and timeout must be positive")

    @property
    def provider(self) -> str:
        return "openai" if self.emotion_mode != "demo" and self.api_key else "demo"


def load_settings(*, emotion_mode: str | None = None) -> Settings:
    load_dotenv(ROOT / ".env", override=False)
    return Settings(
        api_key=os.getenv("OPENAI_API_KEY", "").strip(),
        model=os.getenv("OPENAI_MODEL", "gpt-4o-mini"),
        emotion_mode=emotion_mode or os.getenv("EMOTION_MODE", "auto").lower(),
        timeout_seconds=float(os.getenv("OPENAI_TIMEOUT_SECONDS", "20")),
        max_audio_bytes=int(os.getenv("MAX_AUDIO_BYTES", str(20 * 1024 * 1024))),
        max_audio_seconds=float(os.getenv("MAX_AUDIO_SECONDS", "120")),
    )
