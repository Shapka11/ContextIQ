import asyncio
from types import SimpleNamespace
from unittest.mock import AsyncMock

import pytest
from pydantic import ValidationError

from app.config import Settings
from app.schemas import AnalyzeRequest, MoodAnalysis
from app.services.emotion import EmotionAnalyzer, EmotionUnavailable, demo_analyze


@pytest.mark.parametrize("text,emoji,mood", [
    ("Я счастлив", "😊", "happy"), ("Мне грустно", "", "sad"),
    ("Очень устал", "😴", "stressed"), ("Ура!", "🎉", "excited"),
    ("Посоветуй книгу", "", "neutral"), ("", "😢", "sad"),
])
def test_russian_text_and_emoji(text, emoji, mood):
    result = demo_analyze(AnalyzeRequest(text=text, emoji=emoji))
    assert result.mood == mood
    assert MoodAnalysis.model_validate_json(result.model_dump_json()) == result


def test_themes_and_fatigue():
    request = AnalyzeRequest(text="Я устал после университета, хочу отдохнуть под Человека-паука", emoji="😴")
    result = demo_analyze(request)
    assert result.mood == "stressed"
    assert result.energy == pytest.approx(0.2)
    assert result.themes == ["spider-man", "relax"]


def test_negation_and_repeatability():
    request = AnalyzeRequest(text="Не грущу, просто выбираю книгу")
    assert demo_analyze(request).mood == "neutral"
    assert demo_analyze(request) == demo_analyze(request)


@pytest.mark.parametrize("change", [{"mood": "angry"}, {"energy": 1.1}, {"valence": -2}, {"confidence": -0.1}, {"energy": float("nan")}, {"unknown": 1}])
def test_analysis_json_validation(change):
    payload = {"mood":"neutral", "energy":0.5, "valence":0, "themes":[], "confidence":0.4}
    with pytest.raises(ValidationError):
        MoodAnalysis.model_validate(payload | change)


def test_empty_input_rejected():
    with pytest.raises(ValidationError):
        AnalyzeRequest(text="  ", emoji="")


def test_no_key_auto_uses_demo():
    analyzer = EmotionAnalyzer(Settings())
    assert analyzer.provider == "demo"
    result = asyncio.run(analyzer.analyze(AnalyzeRequest(text="Ура")))
    assert result.mood == "excited"
    assert analyzer._client is None


def test_openai_structured_result_with_mock():
    analyzer = EmotionAnalyzer(Settings(api_key="unit-test-placeholder", emotion_mode="openai"))
    expected = demo_analyze(AnalyzeRequest(text="Ура"))
    parse = AsyncMock(return_value=SimpleNamespace(output_parsed=expected))
    analyzer._client = SimpleNamespace(responses=SimpleNamespace(parse=parse))
    assert asyncio.run(analyzer.analyze(AnalyzeRequest(text="Ура"))) == expected
    assert parse.call_args.kwargs["text_format"] is MoodAnalysis
    assert parse.call_args.kwargs["store"] is False


@pytest.mark.parametrize("parsed", [None, {"mood": "invalid"}])
def test_openai_refusal_or_invalid_structure_with_mock(parsed):
    analyzer = EmotionAnalyzer(Settings(api_key="unit-test-placeholder"))
    analyzer._client = SimpleNamespace(responses=SimpleNamespace(parse=AsyncMock(return_value=SimpleNamespace(output_parsed=parsed))))
    with pytest.raises(EmotionUnavailable):
        asyncio.run(analyzer.analyze(AnalyzeRequest(text="текст")))


def test_settings_invalid_mode_and_no_key():
    with pytest.raises(ValueError):
        Settings(emotion_mode="openai")
    with pytest.raises(ValueError):
        Settings(emotion_mode="unknown")
