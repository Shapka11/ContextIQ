import io
import wave

import numpy as np
import pytest

from app.physics.audio_features import InvalidAudio, amplitude_spectrum, analyze_signal, normalize_samples, read_wav, spectrogram


@pytest.mark.parametrize("frequency", [440, 880])
def test_fft_known_frequency(sine, frequency):
    x = sine(frequency)
    result = analyze_signal(x, 16000)
    frequencies, amplitudes = amplitude_spectrum(x, 16000)
    assert abs(result.dominant_frequency_hz - frequency) <= 5
    assert frequencies[amplitudes.argmax()] == pytest.approx(frequency)
    assert amplitudes.max() == pytest.approx(1, abs=1e-10)
    assert result.spectral_centroid_hz == pytest.approx(frequency, abs=1e-5)
    assert result.duration_seconds == 2


@pytest.mark.parametrize("frequency,duration,rate", [(440.7, 0.4, 16000), (880.3, 0.73, 44100), (439.2, 1.0, 22050)])
def test_off_bin_frequency_error(sine, frequency, duration, rate):
    result = analyze_signal(sine(frequency, duration=duration, rate=rate), rate)
    assert abs(result.dominant_frequency_hz - frequency) <= 5


def test_rms_unit_sine_and_peak(sine):
    result = analyze_signal(sine(), 16000)
    assert result.rms == pytest.approx(1 / np.sqrt(2), abs=1e-10)
    assert result.peak_amplitude == pytest.approx(1)


def test_rms_doubles_with_amplitude(sine):
    low = analyze_signal(sine(amplitude=0.25), 16000)
    high = analyze_signal(sine(amplitude=0.5), 16000)
    assert high.rms == pytest.approx(2 * low.rms)


@pytest.mark.parametrize("count", [1, 2, 16000])
def test_silence(count):
    result = analyze_signal(np.zeros(count), 16000)
    assert result.rms == result.peak_amplitude == result.dominant_frequency_hz == result.spectral_centroid_hz == 0
    _, _, db = spectrogram(np.zeros(count), 16000)
    assert np.all(np.isfinite(db))
    assert np.all(db == -120)


@pytest.mark.parametrize("count", [1, 2, 3, 10])
def test_very_short_audio(count, make_wav):
    audio = read_wav(make_wav(np.ones(count, dtype=np.int16)))
    result = analyze_signal(audio.samples, audio.sample_rate)
    assert result.duration_seconds == count / 16000
    assert result.dominant_frequency_hz == 0
    f, t, db = spectrogram(audio.samples, audio.sample_rate)
    assert db.shape == (f.size, t.size)
    assert np.all(np.isfinite(db))


@pytest.mark.parametrize("dtype,expected", [(np.uint8, [-1, 0, 127 / 128]), (np.int16, [-1, 0, 32767 / 32768]), (np.int32, [-1, 0, (2**31-1)/2**31])])
def test_pcm_numeric_normalization(dtype, expected, make_wav):
    info = np.iinfo(dtype)
    values = np.array([info.min, 128 if dtype == np.uint8 else 0, info.max], dtype=dtype)
    assert read_wav(make_wav(values)).samples == pytest.approx(expected)


def test_pcm24_decoding():
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as stream:
        stream.setnchannels(1)
        stream.setsampwidth(3)
        stream.setframerate(16000)
        stream.writeframes(b"\x00\x00\x80\x00\x00\x00\xff\xff\x7f")
    assert read_wav(buffer.getvalue()).samples == pytest.approx([-1, 0, (2**23-1)/2**23])


def test_stereo_average_before_mixing(make_wav):
    pcm = np.array([[30000, 30000], [20000, -20000]], dtype=np.int16)
    assert read_wav(make_wav(pcm)).samples == pytest.approx([30000/32768, 0])


def test_dc_excluded_from_frequency():
    result = analyze_signal(np.full(100, 0.25), 16000)
    assert result.rms == 0.25
    assert result.dominant_frequency_hz == result.spectral_centroid_hz == 0


def test_stft_has_expected_frequency(sine):
    frequencies, _, db = spectrogram(sine(440, amplitude=0.5), 16000)
    assert abs(frequencies[db[:, 1:-1].mean(axis=1).argmax()] - 440) <= 16000/1024


@pytest.mark.parametrize("data", [b"", b"not a wav", b"RIFF\xff\xff\xff\xffWAVE"])
def test_invalid_wav(data):
    with pytest.raises(InvalidAudio):
        read_wav(data)


def test_truncated_and_empty_wav(make_wav):
    valid = make_wav(np.zeros(100, dtype=np.int16))
    with pytest.raises(InvalidAudio):
        read_wav(valid[:-4])
    with pytest.raises(InvalidAudio):
        read_wav(make_wav(np.array([], dtype=np.int16)))


def test_malformed_fmt_chunk():
    body = b"WAVEfmt " + (1).to_bytes(4, "little") + b"\x01"
    with pytest.raises(InvalidAudio):
        read_wav(b"RIFF" + len(body).to_bytes(4, "little") + body)


def test_audio_limits(make_wav):
    valid = make_wav(np.zeros(1000, dtype=np.int16))
    with pytest.raises(InvalidAudio):
        read_wav(valid, max_bytes=100)
    with pytest.raises(InvalidAudio):
        read_wav(valid, max_seconds=0.001)
    with pytest.raises(InvalidAudio):
        read_wav(make_wav(np.zeros((100, 3), dtype=np.int16)))


@pytest.mark.parametrize("samples", [np.array([np.nan]), np.array([np.inf]), np.array([]), np.array([1.1])])
def test_invalid_numeric_samples(samples):
    with pytest.raises(InvalidAudio):
        normalize_samples(samples)
