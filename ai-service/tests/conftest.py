import io
import json
from pathlib import Path

import numpy as np
import pytest
from scipy.io import wavfile

ROOT = Path(__file__).resolve().parents[1]


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
