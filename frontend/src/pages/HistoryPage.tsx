import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, CalendarDays, Clock } from 'lucide-react'
import { useHistory } from '../hooks/useHistory'
import { MoodCharts } from '../components/charts/MoodCharts'
import { Button } from '../components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '../components/ui/states'
import { dateLabel, moods, percent, tagLabel } from '../lib/utils'
export default function HistoryPage() {
  const history = useHistory()
  const [days, setDays] = useState(7)
  const [now] = useState(() => Date.now())
  const allEntries = history.data?.pages.flatMap((page) => page.items) ?? []
  const entries = allEntries.filter(
    (entry) => days === 0 || Date.parse(entry.createdAt) >= now - days * 86_400_000,
  )
  return (
    <>
      <div className="page-heading">
        <span className="eyebrow">КАЖДЫЙ ДЕНЬ — ЧАСТЬ ТВОЕЙ ИСТОРИИ</span>
        <h1>
          Замечать <span className="gradient-text">себя.</span>
        </h1>
        <p>Наблюдай за своим ритмом, без оценок и ожиданий.</p>
      </div>
      <div className="section-heading history-toolbar">
        <div className="filter-tabs">
          {[7, 30, 0].map((value) => (
            <button
              key={value}
              aria-pressed={value === days}
              className={days === value ? 'active' : ''}
              onClick={() => setDays(value)}
            >
              {value ? `Последние ${value} дней` : 'Все записи'}
            </button>
          ))}
        </div>
        <span className="muted">
          <CalendarDays size={14} /> Всего чек-ин: {history.data?.pages[0].total ?? '—'}
        </span>
      </div>
      {history.isPending ? (
        <Skeleton count={2} />
      ) : history.isError ? (
        <ErrorState
          error={history.error}
          retry={() => {
            void history.refetch()
          }}
        />
      ) : (
        <>
          {entries.length ? (
            <>
              <MoodCharts entries={entries} />
              <div className="section-heading history-list-title">
                <h2>Моменты твоего дня</h2>
                <span className="subtle-label">{entries.length} ЗАГРУЖЕНО ЗА ПЕРИОД</span>
              </div>
              <div className="history-list">
                {[...entries].reverse().map((entry) => (
                  <article className="panel history-item" key={entry.id}>
                    <span className="history-emoji">
                      {entry.emoji || moods[entry.analysis.mood].emoji}
                    </span>
                    <div className="history-item-main">
                      <div className="section-heading">
                        <h3>{moods[entry.analysis.mood].label}</h3>
                        <time dateTime={entry.createdAt}>
                          <Clock size={12} />
                          {dateLabel(entry.createdAt)}
                        </time>
                      </div>
                      <p>{entry.text || 'Чек-ин с эмодзи'}</p>
                      <div className="tags">
                        {entry.analysis.themes.map((theme) => (
                          <span key={theme}>{tagLabel(theme)}</span>
                        ))}
                        <span>Энергия {percent(entry.analysis.energy)}</span>
                        {entry.analysisMode === 'demo' && <span>Анализ по правилам · demo</span>}
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            </>
          ) : (
            <EmptyState
              title="У этой истории будет начало"
              text="За выбранный период нет загруженных записей. Сделай первый чек-ин — сохраним его здесь."
            >
              <Button asChild>
                <Link to="/dashboard">
                  Рассказать о настроении <ArrowRight size={16} />
                </Link>
              </Button>
            </EmptyState>
          )}
          {history.hasNextPage && (
            <div className="load-more">
              <p className="field-hint">
                Backend отдаёт историю от старых записей к новым. Для полного графика и фильтра
                периода загрузите остальные страницы.
              </p>
              <Button
                variant="secondary"
                disabled={history.isFetchingNextPage}
                onClick={() => {
                  void history.fetchNextPage()
                }}
              >
                {history.isFetchingNextPage ? 'Загружаем…' : 'Загрузить ещё 100 записей'}
              </Button>
              {history.isFetchNextPageError && (
                <p role="alert" className="validation">
                  Не удалось загрузить следующую страницу.
                </p>
              )}
            </div>
          )}
          <p className="field-hint">
            Показаны только реальные ответы анализа. История API содержит текст, эмодзи и
            настроение; ранее выданные рекомендации не возвращаются.
          </p>
        </>
      )}
    </>
  )
}
