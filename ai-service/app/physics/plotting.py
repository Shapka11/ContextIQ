from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np

from app.physics.audio_features import amplitude_spectrum, spectrogram


def save_signal_graphs(samples: np.ndarray, sample_rate: int, directory: Path, prefix: str, title: str) -> list[str]:
    directory.mkdir(parents=True, exist_ok=True)
    paths = []
    fig, ax = plt.subplots(figsize=(10, 4), layout="constrained")
    count = min(samples.size, max(1, int(sample_rate * 0.03)))
    ax.plot(np.arange(count) / sample_rate, samples[:count], lw=1)
    ax.set(xlabel="Time (s)", ylabel="Amplitude (FS)", title=f"{title}: waveform (first 30 ms)")
    ax.grid(alpha=0.25)
    path = directory / f"{prefix}_waveform.png"
    fig.savefig(path, dpi=150)
    plt.close(fig)
    paths.append(str(path))
    frequencies, amplitudes = amplitude_spectrum(samples, sample_rate)
    fig, ax = plt.subplots(figsize=(10, 4), layout="constrained")
    ax.plot(frequencies, amplitudes, lw=1)
    ax.set(xlabel="Frequency (Hz)", ylabel="Peak amplitude (FS)", title=f"{title}: Hann amplitude spectrum")
    ax.set_xlim(0, min(sample_rate / 2, 4000))
    ax.grid(alpha=0.25)
    path = directory / f"{prefix}_spectrum.png"
    fig.savefig(path, dpi=150)
    plt.close(fig)
    paths.append(str(path))
    frequencies, times, db = spectrogram(samples, sample_rate)
    fig, ax = plt.subplots(figsize=(10, 4), layout="constrained")
    mesh = ax.pcolormesh(times, frequencies, db, shading="auto", vmin=-120, vmax=0, cmap="magma")
    ax.set(xlabel="Time (s)", ylabel="Frequency (Hz)", title=f"{title}: STFT spectrogram")
    ax.set_ylim(0, min(sample_rate / 2, 4000))
    fig.colorbar(mesh, ax=ax, label="Magnitude (dBFS)")
    path = directory / f"{prefix}_spectrogram.png"
    fig.savefig(path, dpi=150)
    plt.close(fig)
    paths.append(str(path))
    return paths


def save_rms_graph(amplitudes: list[float], measured: list[float], path: Path):
    fig, ax = plt.subplots(figsize=(7, 4), layout="constrained")
    ax.plot(amplitudes, np.asarray(amplitudes) / np.sqrt(2), label="Theory: A / sqrt(2)")
    ax.scatter(amplitudes, measured, marker="x", s=60, color="red", label="Measured (PCM16)", zorder=3)
    ax.set(xlabel="Input sine amplitude (FS)", ylabel="RMS amplitude (FS)", title="RMS versus input amplitude, 440 Hz")
    ax.grid(alpha=0.25)
    ax.legend()
    fig.savefig(path, dpi=150)
    plt.close(fig)


def save_confusion_matrix(matrix: np.ndarray, labels: list[str], path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    fig, ax = plt.subplots(figsize=(7, 6), layout="constrained")
    mesh = ax.imshow(matrix, cmap="Blues", vmin=0)
    for row in range(len(labels)):
        for col in range(len(labels)):
            ax.text(col, row, str(matrix[row, col]), ha="center", va="center", color="black")
    ax.set(xticks=range(len(labels)), yticks=range(len(labels)), xticklabels=labels,
           yticklabels=labels, xlabel="Predicted mood", ylabel="Preliminary reference mood",
           title="Small demo sample: confusion matrix (counts)")
    fig.colorbar(mesh, ax=ax, label="Samples (count)")
    fig.savefig(path, dpi=150)
    plt.close(fig)
