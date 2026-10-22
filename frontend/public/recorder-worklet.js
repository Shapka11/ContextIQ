class PCMRecorder extends AudioWorkletProcessor {
  constructor() {
    super()
    this.active = true
    this.buffer = new Float32Array(4096)
    this.offset = 0
    this.port.onmessage = (e) => {
      if (e.data === 'stop') {
        this.active = false
        if (this.offset) this.port.postMessage(this.buffer.slice(0, this.offset))
        this.port.postMessage('stopped')
      }
    }
  }
  process(inputs) {
    if (!this.active) return false
    const channels = inputs[0]
    if (channels?.[0]) {
      for (let i = 0; i < channels[0].length; i++) {
        let mono = 0
        for (const channel of channels) mono += channel[i] / channels.length
        this.buffer[this.offset++] = mono
        if (this.offset === this.buffer.length) {
          this.port.postMessage(this.buffer.slice())
          this.offset = 0
        }
      }
    }
    return true
  }
}
registerProcessor('pcm-recorder', PCMRecorder)
