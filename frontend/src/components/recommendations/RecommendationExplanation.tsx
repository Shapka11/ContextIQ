import { Sparkles } from 'lucide-react'
import { percent } from '../../lib/utils'

export function RecommendationExplanation({ text }: { text: string }) {
  const match =
    /^Настроение: (\d+\.\d+); предпочтения: (\d+\.\d+); контекст: (\d+\.\d+); новизна: (\d+\.\d+)\.$/.exec(
      text,
    )
  const values = match?.slice(1).map(Number)
  const valid = values?.every((value) => value >= 0 && value <= 1)
  const labels = ['Настроение', 'Предпочтения', 'Контекст', 'Новизна']
  const descriptions = [
    'Совпадение с настроениями материала',
    'Совпадение с тегами и категориями профиля',
    'Близость энергии и совпадение тем',
    'Был ли материал в предыдущих подборках',
  ]
  return (
    <div className="explanation">
      <span>
        <Sparkles size={13} /> Почему тебе подходит
      </span>
      {valid && values ? (
        <>
          <p>Сигналы, которые учёл алгоритм подбора:</p>
          <div className="explanation-metrics">
            {values.map((value, i) => (
              <div key={labels[i]} title={descriptions[i]}>
                <span>
                  {labels[i]}
                  <strong>{percent(value)}</strong>
                </span>
                <div className="explanation-track">
                  <i style={{ width: percent(value) }} />
                </div>
              </div>
            ))}
          </div>
          <details>
            <summary>Ответ сервиса</summary>
            <p>{text}</p>
          </details>
        </>
      ) : (
        <p>{text}</p>
      )}
    </div>
  )
}
