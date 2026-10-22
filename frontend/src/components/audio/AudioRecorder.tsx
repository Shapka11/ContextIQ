import { useEffect, useRef, useState } from 'react'
import { Mic, Square, Upload, LoaderCircle } from 'lucide-react'
import { useMutation } from '@tanstack/react-query'
import { audioApi } from '../../api'
import { useAudioRecorder } from '../../hooks/useAudioRecorder'
import { Button } from '../ui/button'
import { ErrorState } from '../ui/states'
import { AudioAnalysisPanel } from './AudioAnalysisPanel'
export function AudioRecorder() {
  const audio = useAudioRecorder()
  const [open, setOpen] = useState(false)
  const controller = useRef<AbortController | null>(null)
  const analysis = useMutation({
    mutationFn: (blob: Blob) => {
      controller.current = new AbortController()
      return audioApi.analyze(blob, controller.current.signal)
    },
  })
  useEffect(() => () => controller.current?.abort(), [])
  return (
    <div className={`audio-recorder ${audio.recording ? 'is-recording' : ''}`}>
      <button
        type="button"
        className="audio-toggle"
        onClick={() => setOpen(!open)}
        aria-expanded={open}
      >
        <span className="mic-icon">
          <Mic size={18} />
        </span>
        <span>
          <strong>У настроения есть голос</strong>
          <small>Запиши звук и исследуй его физику</small>
        </span>
        <span className="audio-toggle-label">{open ? 'Свернуть' : 'Записать'} ↗</span>
      </button>
      {open && (
        <div className="recorder-content">
          <p className="field-hint">
            Только физический анализ. Распознавание речи не поддерживается. До 120 секунд, PCM WAV.
            Аудио отправится после нажатия «Исследовать звук».
          </p>
          <div className="record-controls">
            {audio.recording ? (
              <Button type="button" variant="destructive" onClick={audio.stop}>
                <Square size={15} /> Остановить · {Math.floor(audio.seconds / 60)}:
                {String(audio.seconds % 60).padStart(2, '0')}
                <span className="record-dot" />
              </Button>
            ) : (
              <Button
                type="button"
                variant="secondary"
                disabled={audio.starting || analysis.isPending}
                onClick={() => {
                  analysis.reset()
                  void audio.start()
                }}
              >
                <Mic size={16} />
                {audio.starting
                  ? 'Подключаем микрофон…'
                  : audio.result
                    ? 'Записать заново'
                    : 'Начать запись'}
              </Button>
            )}
          </div>
          {audio.error && (
            <p className="validation" role="alert">
              {audio.error}
            </p>
          )}
          {audio.result && (
            <>
              <audio controls src={audio.result.url} aria-label="Прослушать запись" />
              <div className="waveform" aria-label="Осциллограмма реальной записи">
                <svg
                  viewBox="0 0 640 90"
                  role="img"
                  aria-label="Амплитуда записанного сигнала во времени"
                >
                  <line x1="0" y1="45" x2="640" y2="45" stroke="currentColor" opacity=".15" />
                  {audio.result.peaks.map((p, i) => (
                    <line
                      key={i}
                      x1={(i * 640) / audio.result!.peaks.length}
                      x2={(i * 640) / audio.result!.peaks.length}
                      y1={45 - p.max * 42}
                      y2={45 - p.min * 42}
                      stroke="currentColor"
                      strokeWidth="2"
                    />
                  ))}
                </svg>
                <span>
                  0 с <span>Осциллограмма · амплитуда (отн. ед.)</span>{' '}
                  {audio.result.duration.toFixed(1)} с
                </span>
              </div>
              <Button
                type="button"
                size="sm"
                variant="secondary"
                disabled={analysis.isPending}
                onClick={() => analysis.mutate(audio.result!.blob)}
              >
                {analysis.isPending ? (
                  <LoaderCircle size={16} className="spin" />
                ) : (
                  <Upload size={16} />
                )}{' '}
                Исследовать звук
              </Button>
            </>
          )}
          {analysis.isError && <ErrorState error={analysis.error} />}
          {analysis.data && <AudioAnalysisPanel features={analysis.data} />}
        </div>
      )}
    </div>
  )
}
