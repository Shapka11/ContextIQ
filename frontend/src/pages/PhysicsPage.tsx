import { Activity, AudioLines, Radio, Waves } from 'lucide-react'
import { AudioRecorder } from '../components/audio/AudioRecorder'
export default function PhysicsPage() {
  return (
    <>
      <div className="page-heading">
        <span className="eyebrow">НЕМНОГО НАУКИ В ТВОЁМ КОНТЕКСТЕ</span>
        <h1>
          У звука есть <span className="gradient-text">характер.</span>
        </h1>
        <p>От колебаний воздуха до чисел. Исследуй, как устроен твой голос.</p>
      </div>
      <section className="physics-hero panel">
        <div>
          <span className="pill">
            <AudioLines size={14} /> ЛАБОРАТОРИЯ ЗВУКА
          </span>
          <h2>
            Слышать.
            <br />
            Измерять.
            <br />
            <span className="gradient-text">Понимать.</span>
          </h2>
          <p>
            Микрофон преобразует колебания давления в электрический сигнал, а аудиосистема — в
            последовательность отсчётов. Так появляется цифровая волна.
          </p>
        </div>
        <div className="physics-wave" aria-hidden="true">
          {Array.from({ length: 48 }, (_, i) => (
            <span
              key={i}
              style={{
                height: `${15 + Math.abs(Math.sin(i * 0.35) * Math.cos(i * 0.07)) * 170}px`,
                animationDelay: `${i * 0.025}s`,
              }}
            />
          ))}
        </div>
      </section>
      <div className="physics-grid">
        {[
          {
            icon: Waves,
            title: 'RMS: энергия в среднем',
            formula: 'RMS = √(Σ xᵢ² / N)',
            text: 'Среднеквадратичная амплитуда описывает эффективную величину сигнала. Чем выше RMS при одинаковых условиях записи, тем больше его средняя мощность. Без калибровки микрофона это не громкость в децибелах звукового давления.',
          },
          {
            icon: Radio,
            title: 'FFT: из времени в частоты',
            formula: 'Xₖ = Σ xₙ · e⁻ⁱ²πᵏⁿ⁄ᴺ',
            text: 'Быстрое преобразование Фурье раскладывает сигнал по частотным компонентам. Доминирующая частота — максимум амплитудного спектра; она не всегда совпадает с основным тоном голоса. Python анализирует центрированный сигнал с окном Ханна.',
          },
          {
            icon: Activity,
            title: 'Спектральный центроид',
            formula: 'fᶜ = Σ fₖ|Xₖ| / Σ |Xₖ|',
            text: 'Частотный «центр тяжести» спектра, измеряемый в герцах. Показывает, в какой части спектра сосредоточена амплитуда. Более высокое значение часто связано с ярким тембром или высокочастотным шумом.',
          },
          {
            icon: AudioLines,
            title: 'Дискретизация и амплитуда',
            formula: 'fₘₐₓ < fₛ / 2',
            text: 'Частота дискретизации — число отсчётов в секунду. Чтобы различать частоты до f, нужна частота дискретизации больше 2f. Запись сохраняется в mono PCM WAV, 16 бит. Пиковая амплитуда около 1 может указывать на близость к цифровому пределу.',
          },
        ].map(({ icon: Icon, title, formula, text }) => (
          <article className="panel physics-explainer" key={title}>
            <span className="soft-icon">
              <Icon size={23} />
            </span>
            <h3>{title}</h3>
            <code>{formula}</code>
            <p>{text}</p>
          </article>
        ))}
      </div>
      <section className="panel physics-lab">
        <div className="section-heading">
          <div>
            <span className="eyebrow">ПОПРОБУЙ САМ</span>
            <h2>Твой маленький эксперимент</h2>
          </div>
          <span className="pill">PCM WAV · до 120 с</span>
        </div>
        <p>
          Запиши протяжный гласный звук, затем сравни со свистом или шумом. Сохраняй одинаковое
          расстояние до микрофона. Осциллограмма строится из твоей записи; шесть численных
          параметров вычисляет сервис через Backend.
        </p>
        <AudioRecorder />
        <div className="science-note">
          Акустические параметры описывают физику звука. Они не позволяют сами по себе достоверно
          определить психологическое состояние. В ContextIQ предположение о настроении строится
          отдельно, по тексту и эмодзи.
        </div>
        <p className="field-hint">
          Волна выше — декоративная иллюстрация. API не возвращает спектр или спектрограмму, поэтому
          графики таких измерений не показаны.
        </p>
      </section>
    </>
  )
}
