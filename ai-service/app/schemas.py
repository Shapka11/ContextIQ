from enum import StrEnum
from typing import Annotated, Literal

from pydantic import BaseModel, ConfigDict, Field, model_validator
from pydantic.alias_generators import to_camel

Unit = Annotated[float, Field(ge=0, le=1, allow_inf_nan=False)]
Tag = Annotated[str, Field(min_length=1, max_length=100)]
Identifier = Annotated[str, Field(min_length=1, max_length=100)]


class Contract(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True, extra="forbid", allow_inf_nan=False)


class Mood(StrEnum):
    happy = "happy"
    sad = "sad"
    stressed = "stressed"
    excited = "excited"
    neutral = "neutral"


class AnalyzeRequest(Contract):
    text: str = Field(default="", max_length=8000)
    emoji: str = Field(default="", max_length=100)

    @model_validator(mode="after")
    def has_content(self):
        if not (self.text.strip() or self.emoji.strip()):
            raise ValueError("Provide text or emoji")
        return self


class MoodAnalysis(Contract):
    mood: Mood
    energy: Unit
    valence: float = Field(ge=-1, le=1)
    themes: list[Tag] = Field(max_length=30)
    confidence: Unit = Field(description="Heuristic certainty in demo mode; not a calibrated probability")


class AudioFeatures(Contract):
    duration_seconds: float = Field(gt=0)
    sample_rate: int = Field(gt=0)
    rms: float = Field(ge=0)
    peak_amplitude: float = Field(ge=0)
    dominant_frequency_hz: float = Field(ge=0)
    spectral_centroid_hz: float = Field(ge=0)


class CatalogItem(Contract):
    id: Identifier
    title: str = Field(min_length=1, max_length=300)
    category: Literal["movie", "music", "book", "activity"]
    tags: list[Tag] = Field(max_length=50)
    moods: list[Mood] = Field(min_length=1, max_length=5)
    energy: Unit


class Preferences(Contract):
    categories: list[Literal["movie", "music", "book", "activity"]] = Field(default_factory=list, max_length=4)
    tags: list[Tag] = Field(default_factory=list, max_length=50)
    seen_ids: list[Identifier] = Field(default_factory=list, max_length=10000)


class RankRequest(Contract):
    analysis: MoodAnalysis
    preferences: Preferences = Field(default_factory=Preferences)
    candidates: list[CatalogItem] = Field(max_length=1000)

    @model_validator(mode="after")
    def unique_ids(self):
        ids = [item.id for item in self.candidates]
        if len(ids) != len(set(ids)):
            raise ValueError("Candidate IDs must be unique")
        return self


class ScoreComponents(Contract):
    mood_match: Unit
    preference_match: Unit
    context_match: Unit
    novelty: Unit


class RankedItem(Contract):
    id: Identifier
    score: Unit
    components: ScoreComponents
    explanation: str
