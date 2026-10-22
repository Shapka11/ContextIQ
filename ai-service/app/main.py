from contextlib import asynccontextmanager
from typing import Annotated

from fastapi import FastAPI, File, HTTPException, Response, UploadFile
from starlette.concurrency import run_in_threadpool

from app.config import Settings, load_settings
from app.physics.audio_features import InvalidAudio, analyze_signal, read_wav
from app.schemas import AnalyzeRequest, AudioFeatures, MoodAnalysis, RankedItem, RankRequest
from app.services.emotion import EmotionAnalyzer, EmotionUnavailable
from app.services.recommender import rank_candidates


def create_app(settings: Settings | None = None) -> FastAPI:
    config = settings or load_settings()
    analyzer = EmotionAnalyzer(config)

    @asynccontextmanager
    async def lifespan(application: FastAPI):
        yield
        await analyzer.close()

    application = FastAPI(title="ContextIQ AI service", version="1.0.0", lifespan=lifespan)
    application.state.analyzer = analyzer

    @application.get("/health")
    def health():
        return {"status": "ok", "textAnalysisMode": analyzer.provider, "version": "1.0.0"}

    @application.post("/internal/analyze", response_model=MoodAnalysis)
    async def analyze(request: AnalyzeRequest, response: Response):
        response.headers["X-Analysis-Mode"] = analyzer.provider
        try:
            return await analyzer.analyze(request)
        except EmotionUnavailable as exc:
            raise HTTPException(status_code=502, detail="Text analysis provider unavailable or invalid response") from exc

    @application.post("/internal/audio-features", response_model=AudioFeatures)
    async def audio_features(file: Annotated[UploadFile, File(description="PCM WAV mono/stereo")]):
        try:
            data = await file.read(config.max_audio_bytes + 1)
        finally:
            await file.close()
        if len(data) > config.max_audio_bytes:
            raise HTTPException(status_code=413, detail="Audio upload size limit exceeded")

        def compute():
            audio = read_wav(data, max_bytes=config.max_audio_bytes, max_seconds=config.max_audio_seconds)
            return analyze_signal(audio.samples, audio.sample_rate)

        try:
            return await run_in_threadpool(compute)
        except InvalidAudio as exc:
            raise HTTPException(status_code=422, detail=str(exc)) from exc

    @application.post("/internal/rank", response_model=list[RankedItem])
    def rank(request: RankRequest):
        return rank_candidates(request)

    return application


app = create_app()
