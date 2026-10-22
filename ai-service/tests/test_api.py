import numpy as np
import pytest
from fastapi.testclient import TestClient

from app.config import Settings
from app.main import create_app
from app.schemas import MoodAnalysis
from app.services.emotion import EmotionUnavailable


@pytest.fixture
def client():
    with TestClient(create_app(Settings(emotion_mode="demo"))) as connection:
        yield connection


def test_health_and_openapi(client):
    assert client.get("/health").json()["textAnalysisMode"] == "demo"
    schema = client.get("/openapi.json").json()
    assert set(schema["components"]["schemas"]["AudioFeatures"]["properties"]) == {
        "durationSeconds", "sampleRate", "rms", "peakAmplitude", "dominantFrequencyHz", "spectralCentroidHz"}


def test_analyze_endpoint_and_stability(client):
    payload = {"text":"Я устал, хочу отдохнуть под Человека-паука", "emoji":"😴"}
    response = client.post("/internal/analyze", json=payload)
    assert response.status_code == 200
    assert response.headers["X-Analysis-Mode"] == "demo"
    assert MoodAnalysis.model_validate(response.json()).mood == "stressed"
    assert response.json() == client.post("/internal/analyze", json=payload).json()


@pytest.mark.parametrize("body", [{}, {"text":""}, {"text":"a"*8001}, {"text":"hello", "unknown":1}])
def test_analyze_invalid_json(client, body):
    assert client.post("/internal/analyze", json=body).status_code == 422


def test_audio_endpoint(client, sine, make_wav):
    pcm = np.rint(sine(440, amplitude=0.5) * 32767).astype(np.int16)
    response = client.post("/internal/audio-features", files={"file": ("tone.wav", make_wav(pcm), "audio/wav")})
    assert response.status_code == 200
    assert response.json()["dominantFrequencyHz"] == 440
    assert response.json()["rms"] == pytest.approx(0.5 / np.sqrt(2), abs=5e-5)


def test_silence_endpoint(client, make_wav):
    response = client.post("/internal/audio-features", files={"file": ("silence.wav", make_wav(np.zeros(10, dtype=np.int16)), "audio/wav")})
    assert response.status_code == 200
    assert response.json()["rms"] == response.json()["spectralCentroidHz"] == 0


def test_invalid_audio_endpoint(client):
    assert client.post("/internal/audio-features", files={"file": ("bad.wav", b"broken", "audio/wav")}).status_code == 422
    assert client.post("/internal/audio-features").status_code == 422


def test_upload_size_limit():
    with TestClient(create_app(Settings(emotion_mode="demo", max_audio_bytes=16))) as connection:
        assert connection.post("/internal/audio-features", files={"file": ("big.wav", b"a" * 17)}).status_code == 413


def test_rank_endpoint(client, catalog):
    analysis = client.post("/internal/analyze", json={"text":"Я устал, хочу отдохнуть"}).json()
    body = {"analysis":analysis, "preferences":{"categories":["music"], "tags":["relax"]}, "candidates":catalog}
    response = client.post("/internal/rank", json=body)
    assert response.status_code == 200
    assert len(response.json()) == 5
    assert "moodMatch" in response.json()[0]["components"]
    assert response.json() == client.post("/internal/rank", json=body).json()
    body["analysis"]["energy"] = 10
    assert client.post("/internal/rank", json=body).status_code == 422


def test_provider_error_is_502_and_safe(client, monkeypatch):
    async def fail(request):
        raise EmotionUnavailable("Sensitive provider details")
    monkeypatch.setattr(client.app.state.analyzer, "analyze", fail)
    response = client.post("/internal/analyze", json={"text":"test"})
    assert response.status_code == 502
    assert "Sensitive" not in response.text
