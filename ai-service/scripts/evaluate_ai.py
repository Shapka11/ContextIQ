import argparse
import asyncio
import json
import sys
from datetime import datetime, timezone
from pathlib import Path
from time import perf_counter

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

import numpy as np

from app.config import load_settings
from app.physics.plotting import save_confusion_matrix
from app.schemas import AnalyzeRequest, Mood
from app.services.emotion import EmotionAnalyzer


def calculate_metrics(matrix: np.ndarray) -> tuple[float, float]:
    accuracy = float(np.trace(matrix) / matrix.sum()) if matrix.sum() else 0.0
    f1 = []
    for index in range(matrix.shape[0]):
        tp = matrix[index, index]
        denominator = matrix[index, :].sum() + matrix[:, index].sum()
        f1.append(float(2 * tp / denominator) if denominator else 0.0)
    return accuracy, float(np.mean(f1))


async def evaluate(mode: str):
    settings = load_settings(emotion_mode=mode)
    analyzer = EmotionAnalyzer(settings)
    dataset = json.loads((ROOT / "data/evaluation/text_samples.json").read_text(encoding="utf-8"))
    labels = [mood.value for mood in Mood]
    matrix = np.zeros((len(labels), len(labels)), dtype=int)
    predictions, latencies = [], []
    try:
        for sample in dataset["samples"]:
            request = AnalyzeRequest(text=sample["text"], emoji=sample.get("emoji", ""))
            started = perf_counter()
            result = await analyzer.analyze(request)
            elapsed = (perf_counter() - started) * 1000
            latencies.append(elapsed)
            matrix[labels.index(sample["expectedMood"]), labels.index(result.mood.value)] += 1
            predictions.append({"id": sample["id"], "expectedMood": sample["expectedMood"],
                                "result": result.model_dump(by_alias=True), "latencyMs": elapsed})
    finally:
        await analyzer.close()
    accuracy, macro_f1 = calculate_metrics(matrix)
    report = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(), "mode": analyzer.provider,
        "model": settings.model if analyzer.provider == "openai" else "deterministic-keyword-emoji-rules-v1",
        "datasetDescription": dataset["description"], "sampleCount": len(predictions),
        "accuracy": accuracy, "macroF1": macro_f1, "labels": labels, "confusionMatrix": matrix.tolist(),
        "meanResponseTimeMs": float(np.mean(latencies)), "p95ResponseTimeMs": float(np.percentile(latencies, 95)),
        "latencyScope": "Sequential analyzer calls with Pydantic output validation; includes first-call overhead, excludes HTTP transport. One pass, no warmup.",
        "predictions": predictions,
    }
    graph = ROOT / "reports/graphs/confusion_matrix.png"
    save_confusion_matrix(matrix, labels, graph)
    (ROOT / "reports/ai_metrics.json").write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False), encoding="utf-8")
    print(json.dumps({key: report[key] for key in ("mode", "sampleCount", "accuracy", "macroF1", "meanResponseTimeMs", "p95ResponseTimeMs")}, ensure_ascii=False))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Evaluate text analysis on the demo dataset")
    parser.add_argument("--mode", choices=("demo", "auto", "openai"), default="demo", help="Default demo makes no API calls")
    asyncio.run(evaluate(parser.parse_args().mode))
