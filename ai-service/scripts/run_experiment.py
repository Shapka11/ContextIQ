import io
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

import numpy as np
from scipy.io import wavfile

from app.physics.audio_features import InvalidAudio, analyze_signal, read_wav
from app.physics.plotting import save_rms_graph, save_signal_graphs


def main():
    graphs = ROOT / "reports/graphs"
    synthetic_dir = ROOT / "reports/synthetic_audio"
    graphs.mkdir(parents=True, exist_ok=True)
    synthetic_dir.mkdir(parents=True, exist_ok=True)
    rate, duration = 16000, 2.0
    amplitudes = [0.1, 0.25, 0.5, 0.75, 1.0]
    rows, graph_paths = [], []
    for frequency in (440.0, 880.0):
        for amplitude in amplitudes:
            t = np.arange(round(duration * rate)) / rate
            x = amplitude * np.sin(2 * np.pi * frequency * t)
            pcm = np.rint(x * 32767).astype(np.int16)
            buffer = io.BytesIO()
            wavfile.write(buffer, rate, pcm)
            name = f"sine_{int(frequency)}Hz_A{amplitude:g}"
            (synthetic_dir / f"{name}.wav").write_bytes(buffer.getvalue())
            audio = read_wav(buffer.getvalue())
            features = analyze_signal(audio.samples, audio.sample_rate)
            error = abs(features.dominant_frequency_hz - frequency)
            expected_rms = amplitude / np.sqrt(2)
            rms_error = abs(features.rms - expected_rms)
            rows.append({
                "name": name, "knownFrequencyHz": frequency, "inputAmplitude": amplitude,
                "features": features.model_dump(by_alias=True), "fftBinWidthHz": rate / audio.samples.size,
                "absoluteFrequencyErrorHz": error, "relativeFrequencyError": error / frequency,
                "expectedRms": expected_rms, "absoluteRmsError": rms_error,
                "rmsWithinTolerance": bool(rms_error <= 5e-5), "frequencyWithinTolerance": error <= 5,
            })
            if amplitude == 0.5:
                graph_paths.extend(save_signal_graphs(audio.samples, rate, graphs, name, f"Synthetic {frequency:g} Hz, A={amplitude}"))
    rms_path = graphs / "rms_vs_amplitude.png"
    save_rms_graph(amplitudes, [row["features"]["rms"] for row in rows if row["knownFrequencyHz"] == 440], rms_path)
    graph_paths.append(str(rms_path))
    recordings, errors = [], []
    for index, path in enumerate(sorted((ROOT / "data/audio").glob("*"))):
        if path.suffix.lower() != ".wav" or not path.is_file():
            continue
        try:
            with path.open("rb") as stream:
                audio = read_wav(stream.read(20 * 1024 * 1024 + 1))
            features = analyze_signal(audio.samples, audio.sample_rate)
            real_graphs = save_signal_graphs(audio.samples, audio.sample_rate, graphs, f"recording_{index:02d}", f"Real recording {index + 1}")
            graph_paths.extend(real_graphs)
            recordings.append({"file": path.relative_to(ROOT).as_posix(), "features": features.model_dump(by_alias=True),
                               "graphs": [Path(p).relative_to(ROOT).as_posix() for p in real_graphs],
                               "note": "User-provided recording; voice/content not automatically verified; no known reference frequency."})
        except (InvalidAudio, OSError) as exc:
            errors.append({"file": path.name, "error": str(exc)})
    passed = all(row["rmsWithinTolerance"] and row["frequencyWithinTolerance"] for row in rows)
    result = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(), "sampleRate": rate,
        "durationSeconds": duration, "encoding": "PCM16, round(A*sin*32767), decoded /32768",
        "frequencyToleranceHz": 5, "rmsTolerance": 5e-5, "checksPassed": passed,
        "syntheticSignals": rows, "realRecordings": recordings, "recordingErrors": errors,
        "realRecordingsNote": "No real recordings supplied" if not recordings else "Separate from synthetic measurements",
        "graphs": [Path(path).relative_to(ROOT).as_posix() for path in graph_paths],
    }
    (ROOT / "reports/physics_metrics.json").write_text(json.dumps(result, ensure_ascii=False, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps({"syntheticCount": len(rows), "realRecordingCount": len(recordings), "checksPassed": passed, "graphs": len(graph_paths)}, ensure_ascii=False))
    if not passed or errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
