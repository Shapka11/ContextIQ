export function encodeWav(samples: Float32Array, sampleRate: number): Blob {
  const buffer = new ArrayBuffer(44 + samples.length * 2)
  const view = new DataView(buffer)
  const write = (offset: number, value: string) => {
    for (let i = 0; i < value.length; i++) view.setUint8(offset + i, value.charCodeAt(i))
  }
  write(0, 'RIFF')
  view.setUint32(4, 36 + samples.length * 2, true)
  write(8, 'WAVE')
  write(12, 'fmt ')
  view.setUint32(16, 16, true)
  view.setUint16(20, 1, true)
  view.setUint16(22, 1, true)
  view.setUint32(24, sampleRate, true)
  view.setUint32(28, sampleRate * 2, true)
  view.setUint16(32, 2, true)
  view.setUint16(34, 16, true)
  write(36, 'data')
  view.setUint32(40, samples.length * 2, true)
  samples.forEach((sample, i) => {
    const clamped = Math.max(-1, Math.min(1, sample))
    view.setInt16(44 + i * 2, Math.round(clamped * (clamped < 0 ? 32768 : 32767)), true)
  })
  return new Blob([buffer], { type: 'audio/wav' })
}
export function waveformPeaks(samples: Float32Array, count = 160) {
  const step = Math.max(1, Math.ceil(samples.length / count))
  return Array.from({ length: Math.min(count, samples.length) }, (_, i) => {
    let min = 0
    let max = 0
    for (let j = i * step; j < Math.min(samples.length, (i + 1) * step); j++) {
      min = Math.min(min, samples[j])
      max = Math.max(max, samples[j])
    }
    return { index: i, min, max }
  })
}
