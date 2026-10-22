import { describe, expect, it } from 'vitest'
import { encodeWav, waveformPeaks } from '../src/lib/wav'

describe('PCM WAV for the Python contract', () => {
  it('writes a valid mono PCM16 RIFF header and clips full scale', async () => {
    const samples = new Float32Array([-2, -1, 0, 0.5, 1, 2])
    const blob = encodeWav(samples, 48000)
    const buffer = await blob.arrayBuffer()
    const view = new DataView(buffer)
    expect(blob.type).toBe('audio/wav')
    expect(new TextDecoder().decode(buffer.slice(0, 4))).toBe('RIFF')
    expect(new TextDecoder().decode(buffer.slice(8, 12))).toBe('WAVE')
    expect(view.getUint32(4, true)).toBe(buffer.byteLength - 8)
    expect(view.getUint16(20, true)).toBe(1)
    expect(view.getUint16(22, true)).toBe(1)
    expect(view.getUint32(24, true)).toBe(48000)
    expect(view.getUint16(34, true)).toBe(16)
    expect(view.getUint32(40, true)).toBe(samples.length * 2)
    expect(Array.from(samples, (_, i) => view.getInt16(44 + i * 2, true)))
      .toEqual([-32768, -32768, 0, 16384, 32767, 32767])
  })

  it('preserves a synthetic 440 Hz sine and RMS within PCM16 quantization', async () => {
    const fs = 48000
    const samples = Float32Array.from({ length: fs }, (_, n) => 0.5 * Math.sin(2 * Math.PI * 440 * n / fs))
    const view = new DataView(await encodeWav(samples, fs).arrayBuffer())
    const decoded = Array.from(samples, (_, i) => view.getInt16(44 + 2 * i, true) / 32768)
    const rms = Math.sqrt(decoded.reduce((sum, x) => sum + x * x, 0) / fs)
    expect(rms).toBeCloseTo(0.5 / Math.sqrt(2), 4)
    expect(Math.max(...decoded)).toBeCloseTo(0.5, 4)
  })

  it('builds waveform envelopes from actual samples including tails and silence', () => {
    expect(waveformPeaks(new Float32Array(), 2)).toEqual([])
    expect(waveformPeaks(new Float32Array([-0.7, 0.5, -0.2]), 2)).toEqual([
      { index: 0, min: expect.closeTo(-0.7), max: 0.5 },
      { index: 1, min: expect.closeTo(-0.2), max: 0 },
    ])
    expect(waveformPeaks(new Float32Array(3), 2).every(p => p.min === 0 && p.max === 0)).toBe(true)
  })
})
