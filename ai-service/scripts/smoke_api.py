import json
import os
import platform
import socket
import subprocess
import sys
from datetime import datetime, timezone
from importlib.metadata import version
from pathlib import Path
from time import monotonic, sleep

import httpx

ROOT = Path(__file__).resolve().parents[1]


def main():
    reports = ROOT / "reports"
    reports.mkdir(exist_ok=True)
    with socket.socket() as temporary:
        temporary.bind(("127.0.0.1", 0))
        port = temporary.getsockname()[1]
    environment = os.environ.copy()
    environment.update({"EMOTION_MODE": "auto", "OPENAI_API_KEY": "", "PYTHONIOENCODING": "utf-8"})
    flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
    with (reports / "uvicorn_smoke.log").open("w", encoding="utf-8") as log:
        process = subprocess.Popen([sys.executable, "-m", "uvicorn", "app.main:app", "--host", "127.0.0.1", "--port", str(port)],
                                   cwd=ROOT, env=environment, stdout=log, stderr=subprocess.STDOUT, creationflags=flags)
        try:
            with httpx.Client(base_url=f"http://127.0.0.1:{port}", timeout=10, trust_env=False) as client:
                deadline = monotonic() + 30
                while True:
                    if process.poll() is not None:
                        raise RuntimeError("Uvicorn exited; see reports/uvicorn_smoke.log")
                    try:
                        health = client.get("/health")
                        health.raise_for_status()
                        break
                    except httpx.TransportError:
                        if monotonic() >= deadline:
                            raise RuntimeError("Uvicorn startup timeout")
                        sleep(0.1)
                assert health.json()["textAnalysisMode"] == "demo"
                request = {"text":"Я устал после университета, хочу отдохнуть под Человека-паука", "emoji":"😴"}
                analysis = client.post("/internal/analyze", json=request)
                analysis.raise_for_status()
                assert analysis.json()["mood"] == "stressed"
                wav = reports / "synthetic_audio/sine_440Hz_A0.5.wav"
                with wav.open("rb") as file:
                    audio = client.post("/internal/audio-features", files={"file": (wav.name, file, "audio/wav")})
                audio.raise_for_status()
                assert abs(audio.json()["dominantFrequencyHz"] - 440) <= 5
                body = {"analysis":analysis.json(), "preferences":{"categories":["music"], "tags":["relax"]},
                        "candidates":json.loads((ROOT / "data/catalog.json").read_text(encoding="utf-8"))}
                ranked = client.post("/internal/rank", json=body)
                ranked.raise_for_status()
                assert len(ranked.json()) == 5
                assert ranked.json() == client.post("/internal/rank", json=body).json()
                invalid = client.post("/internal/audio-features", files={"file": ("invalid.wav", b"invalid", "audio/wav")})
                assert invalid.status_code == 422
                schema = client.get("/openapi.json")
                schema.raise_for_status()
                (reports / "openapi.json").write_text(json.dumps(schema.json(), ensure_ascii=False, indent=2), encoding="utf-8")
                results = {"generatedAtUtc":datetime.now(timezone.utc).isoformat(), "transport":"real HTTP over TCP to Uvicorn", "passed":True,
                           "pythonVersion":platform.python_version(), "platform":platform.platform(),
                           "dependencies":{name:version(name) for name in ("fastapi", "pydantic", "numpy", "scipy", "matplotlib", "pytest", "httpx", "openai", "uvicorn")},
                           "health":{"statusCode":health.status_code, "response":health.json()},
                           "analyze":{"statusCode":analysis.status_code, "request":request, "response":analysis.json(), "mode":analysis.headers["X-Analysis-Mode"]},
                           "audioFeatures":{"statusCode":audio.status_code, "response":audio.json()},
                           "rank":{"statusCode":ranked.status_code, "response":ranked.json()},
                           "invalidWav":{"statusCode":invalid.status_code}}
                (reports / "api_smoke.json").write_text(json.dumps(results, ensure_ascii=False, indent=2, allow_nan=False), encoding="utf-8")
                print(json.dumps({"passed":True, "checked":["/health", "/internal/analyze", "/internal/audio-features", "/internal/rank", "/openapi.json"], "mode":"demo"}))
        finally:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


if __name__ == "__main__":
    main()
