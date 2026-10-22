import { Activity, AudioLines, Clock, Radio, Waves, Zap } from 'lucide-react'
import type { AudioFeatures } from '../../types/api'
import { DEMO_MODE } from '../../lib/session'
export function AudioAnalysisPanel({ features }: { features: AudioFeatures }) {
  const items = [
    { label: 'Длительность', value: features.durationSeconds.toFixed(2), unit: 'с', icon: Clock },
    {
      label: 'Дискретизация',
      value: (features.sampleRate / 1000).toFixed(1),
      unit: 'кГц',
      icon: AudioLines,
    },
    { label: 'RMS', value: features.rms.toFixed(4), unit: 'отн. ед.', icon: Waves },
    {
      label: 'Пиковая амплитуда',
      value: features.peakAmplitude.toFixed(4),
      unit: 'отн. ед.',
      icon: Zap,
    },
    {
      label: 'Доминирующая частота',
      value: features.dominantFrequencyHz.toFixed(1),
      unit: 'Гц',
      icon: Radio,
    },
    {
      label: 'Спектральный центроид',
      value: features.spectralCentroidHz.toFixed(1),
      unit: 'Гц',
      icon: Activity,
    },
  ]
  return (
    <section className="audio-analysis" aria-label="Физические характеристики звука">
      <h3>{DEMO_MODE ? 'Тестовый сигнал: синус 440 Гц, 2 с' : 'Звук в числах'}</h3>
      {DEMO_MODE && (
        <p className="field-hint">Фиксированные данные демонстрации; не измерения вашей записи.</p>
      )}
      <div className="audio-metrics">
        {items.map(({ label, value, unit, icon: Icon }) => (
          <div key={label}>
            <Icon size={15} />
            <span>{label}</span>
            <strong>
              {value} <small>{unit}</small>
            </strong>
          </div>
        ))}
      </div>
      <p className="field-hint">
        Амплитуды нормированы к цифровой шкале, не измерены в дБ SPL. Эти параметры описывают сигнал
        и сами по себе не определяют эмоции.
      </p>
    </section>
  )
}
