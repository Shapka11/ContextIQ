import io
import json
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
import pytest
from scipy.io import wavfile

ROOT = Path(__file__).resolve().parents[1]
RESULTS = []


@pytest.fixture
def make_wav():
    def encode(samples, rate=16000):
        buffer = io.BytesIO()
        wavfile.write(buffer, rate, samples)
        return buffer.getvalue()
    return encode


@pytest.fixture
def sine():
    def generate(frequency=440, amplitude=1.0, duration=2.0, rate=16000):
        return amplitude * np.sin(2 * np.pi * frequency * np.arange(round(rate * duration)) / rate)
    return generate


@pytest.fixture
def catalog():
    return json.loads((ROOT / "data/catalog.json").read_text(encoding="utf-8"))


def pytest_runtest_logreport(report):
    if report.when == "call" or (report.when in ("setup", "teardown") and report.failed):
        RESULTS.append({"test": report.nodeid, "phase": report.when, "outcome": report.outcome, "durationSeconds": report.duration})


def pytest_sessionfinish(session, exitstatus):
    report_dir = ROOT / "reports"
    report_dir.mkdir(exist_ok=True)
    payload = {"generatedAtUtc": datetime.now(timezone.utc).isoformat(), "exitCode": int(exitstatus),
               "passed": sum(row["outcome"] == "passed" for row in RESULTS),
               "failed": sum(row["outcome"] == "failed" for row in RESULTS), "tests": RESULTS}
    (report_dir / "test_results.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
