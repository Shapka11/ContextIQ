import { useCallback, useEffect, useRef, useState } from 'react'
import { encodeWav, waveformPeaks } from '../lib/wav'
interface Recording {
  blob: Blob
  url: string
  sampleRate: number
  duration: number
  peaks: ReturnType<typeof waveformPeaks>
}
export function useAudioRecorder() {
  const [recording, setRecording] = useState(false)
  const [starting, setStarting] = useState(false)
  const [seconds, setSeconds] = useState(0)
  const [error, setError] = useState('')
  const [result, setResult] = useState<Recording | null>(null)
  const resources = useRef<{
    context: AudioContext
    stream: MediaStream
    node: AudioWorkletNode
    source: MediaStreamAudioSourceNode
  } | null>(null)
  const mounted = useRef(true)
  const chunks = useRef<Float32Array[]>([])
  const size = useRef(0)
  const url = useRef('')
  const startTime = useRef(0)
  const stopping = useRef(false)
  const release = useCallback(() => {
    const current = resources.current
    if (current) {
      current.node.disconnect()
      current.source.disconnect()
      current.stream.getTracks().forEach((track) => track.stop())
      void current.context.close()
      resources.current = null
    }
  }, [])
  useEffect(() => {
    mounted.current = true
    return () => {
      mounted.current = false
      release()
      if (url.current) URL.revokeObjectURL(url.current)
    }
  }, [release])
  const stop = useCallback(() => {
    if (!resources.current || stopping.current) return
    stopping.current = true
    resources.current.node.port.postMessage('stop')
  }, [])
  const start = async () => {
    setError('')
    setStarting(true)
    if (!navigator.mediaDevices?.getUserMedia || !window.AudioContext || !window.AudioWorkletNode) {
      setError(
        'Запись недоступна. Откройте приложение на localhost или через HTTPS в современном браузере.',
      )
      setStarting(false)
      return
    }
    let stream: MediaStream | undefined
    let context: AudioContext | undefined
    try {
      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          channelCount: 1,
          echoCancellation: false,
          noiseSuppression: false,
          autoGainControl: false,
        },
      })
      if (!mounted.current) {
        stream.getTracks().forEach((t) => t.stop())
        return
      }
      context = new AudioContext({ sampleRate: 48000 })
      await context.audioWorklet.addModule('/recorder-worklet.js')
      await context.resume()
      if (!mounted.current) {
        stream.getTracks().forEach((t) => t.stop())
        await context.close()
        return
      }
      const source = context.createMediaStreamSource(stream)
      const node = new AudioWorkletNode(context, 'pcm-recorder')
      const sampleRate = context.sampleRate
      chunks.current = []
      size.current = 0
      stopping.current = false
      setSeconds(0)
      setResult(null)
      if (url.current) {
        URL.revokeObjectURL(url.current)
        url.current = ''
      }
      node.port.onmessage = (event) => {
        if (!mounted.current) return
        if (event.data === 'stopped') {
          const data = new Float32Array(size.current)
          let offset = 0
          for (const chunk of chunks.current) {
            data.set(chunk, offset)
            offset += chunk.length
          }
          release()
          setRecording(false)
          if (!data.length) {
            setError('Не удалось получить звук. Попробуйте записать ещё раз.')
            return
          }
          const blob = encodeWav(data, sampleRate)
          url.current = URL.createObjectURL(blob)
          setResult({
            blob,
            url: url.current,
            sampleRate,
            duration: data.length / sampleRate,
            peaks: waveformPeaks(data),
          })
          chunks.current = []
        } else if (event.data instanceof Float32Array) {
          const chunk = event.data.slice(0, Math.max(0, sampleRate * 120 - size.current))
          chunks.current.push(chunk)
          size.current += chunk.length
          if (size.current >= sampleRate * 120) stop()
        }
      }
      resources.current = { context, stream, source, node }
      source.connect(node)
      node.connect(context.destination)
      startTime.current = Date.now()
      setRecording(true)
    } catch (e) {
      stream?.getTracks().forEach((t) => t.stop())
      if (context && context.state !== 'closed') await context.close()
      if (mounted.current)
        setError(
          e instanceof DOMException && e.name === 'NotAllowedError'
            ? 'Доступ к микрофону запрещён. Разрешите его в настройках браузера.'
            : 'Микрофон недоступен. Проверьте подключение и разрешения.',
        )
    } finally {
      if (mounted.current) setStarting(false)
    }
  }
  useEffect(() => {
    if (!recording) return
    const timer = setInterval(() => {
      const elapsed = Math.floor((Date.now() - startTime.current) / 1000)
      setSeconds(Math.min(elapsed, 120))
      if (elapsed >= 120) stop()
    }, 250)
    return () => clearInterval(timer)
  }, [recording, stop])
  return { recording, starting, seconds, error, result, start, stop }
}
