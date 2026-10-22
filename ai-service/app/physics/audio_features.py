import io
import struct
import wave
import warnings
from dataclasses import dataclass

import numpy as np
from numpy.typing import NDArray
from scipy import fft, signal
from scipy.io import wavfile

from app.schemas import AudioFeatures


class InvalidAudio(ValueError):
    pass


@dataclass(frozen=True)
class AudioSignal:
    samples: NDArray[np.float64]
    sample_rate: int


def normalize_samples(samples: np.ndarray) -> NDArray[np.float64]:
    values = np.asarray(samples)
    if values.size == 0 or values.ndim not in (1, 2):
        raise InvalidAudio("Audio must contain nonempty mono/stereo frames")
    if values.ndim == 2 and values.shape[1] not in (1, 2):
        raise InvalidAudio("Only mono/stereo audio is supported")
    if values.dtype == np.uint8:
        normalized = (values.astype(np.float64) - 128) / 128
    elif values.dtype.kind == "i" and values.dtype.itemsize in (2, 4, 8):
        normalized = values.astype(np.float64) / float(2 ** (8 * values.dtype.itemsize - 1))
    elif values.dtype.kind == "f":
        normalized = values.astype(np.float64)
    else:
        raise InvalidAudio("Unsupported audio numeric type")
    if not np.all(np.isfinite(normalized)) or np.any(np.abs(normalized) > 1):
        raise InvalidAudio("Samples must be finite and within digital full scale [-1, 1]")
    return normalized.mean(axis=1) if normalized.ndim == 2 else normalized


def read_wav(data: bytes, *, max_bytes: int = 20 * 1024 * 1024, max_seconds: float = 120) -> AudioSignal:
    if not data or len(data) > max_bytes:
        raise InvalidAudio("WAV is empty or exceeds the upload limit")
    if len(data) < 12 or data[:4] != b"RIFF" or data[8:12] != b"WAVE":
        raise InvalidAudio("Expected a little-endian RIFF/WAVE PCM file")
    if int.from_bytes(data[4:8], "little") + 8 != len(data):
        raise InvalidAudio("WAV RIFF length is invalid or truncated")
    try:
        with wave.open(io.BytesIO(data), "rb") as stream:
            rate, count = stream.getframerate(), stream.getnframes()
            channels, width = stream.getnchannels(), stream.getsampwidth()
            if channels not in (1, 2) or width not in (1, 2, 3, 4):
                raise InvalidAudio("Supported PCM: mono/stereo, 8/16/24/32 bits")
            if not 1 <= rate <= 192000 or count < 1 or count / rate > max_seconds:
                raise InvalidAudio("Invalid sample rate, empty audio or duration limit exceeded")
            raw = stream.readframes(count)
            if len(raw) != count * channels * width:
                raise InvalidAudio("WAV sample data is truncated")
        with warnings.catch_warnings():
            warnings.simplefilter("error", wavfile.WavFileWarning)
            sample_rate, samples = wavfile.read(io.BytesIO(data))
        if sample_rate != rate or samples.shape[0] != count:
            raise InvalidAudio("Inconsistent WAV metadata")
        return AudioSignal(normalize_samples(samples), rate)
    except (wave.Error, EOFError, ValueError, OSError, struct.error, wavfile.WavFileWarning) as exc:
        raise InvalidAudio("Invalid or unsupported PCM WAV file") from exc


def amplitude_spectrum(samples: np.ndarray, sample_rate: int) -> tuple[np.ndarray, np.ndarray]:
    x = normalize_samples(samples)
    if sample_rate <= 0:
        raise InvalidAudio("Sample rate must be positive")
    n = x.size
    ac = x - np.mean(x)
    window = signal.windows.hann(n, sym=False) if n >= 3 else np.ones(n)
    amplitudes = np.abs(fft.rfft(ac * window)) / window.sum()
    if n % 2 == 0:
        amplitudes[1:-1] *= 2
    else:
        amplitudes[1:] *= 2
    amplitudes[0] = 0
    if np.max(np.abs(ac)) <= 1e-12:
        amplitudes[:] = 0
    return fft.rfftfreq(n, 1 / sample_rate), amplitudes


def analyze_signal(samples: np.ndarray, sample_rate: int) -> AudioFeatures:
    x = normalize_samples(samples)
    frequencies, amplitudes = amplitude_spectrum(x, sample_rate)
    total = float(amplitudes.sum())
    return AudioFeatures(
        duration_seconds=x.size / sample_rate,
        sample_rate=sample_rate,
        rms=float(np.sqrt(np.mean(x * x))),
        peak_amplitude=float(np.max(np.abs(x))),
        dominant_frequency_hz=float(frequencies[np.argmax(amplitudes)]) if total else 0,
        spectral_centroid_hz=float(np.dot(frequencies, amplitudes) / total) if total else 0,
    )


def spectrogram(samples: np.ndarray, sample_rate: int) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    x = normalize_samples(samples)
    if sample_rate <= 0:
        raise InvalidAudio("Sample rate must be positive")
    nperseg = max(2, min(1024, x.size))
    if x.size < nperseg:
        x = np.pad(x, (0, nperseg - x.size))
    frequencies, times, z = signal.stft(
        x, fs=sample_rate, window="hann", nperseg=nperseg,
        noverlap=nperseg // 2, boundary="zeros", padded=True, scaling="spectrum",
    )
    magnitude = np.abs(z)
    if nperseg % 2 == 0:
        magnitude[1:-1] *= 2
    else:
        magnitude[1:] *= 2
    return frequencies, times, 20 * np.log10(np.maximum(magnitude, 1e-6))
