import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowRight, AudioLines, BookOpen, Film, Headphones, Leaf, Sparkles } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { recommendationApi } from '../api'
import { Orb } from '../components/layout/Orb'
import { MoodScanner } from '../components/mood/MoodScanner'
import { useHistory } from '../hooks/useHistory'
import { moods, percent } from '../lib/utils'
const prompts = [
  'Устал после университета, хочу расслабиться под атмосферу Человека-паука',
  'Хочется вдохновения и новых идей',
  'У меня отличный день! Хочу сохранить это чувство',
]
export default function DashboardPage() {
  const [example, setExample] = useState('')
  const history = useHistory()
  const catalogue = useQuery({
    queryKey: ['catalog'],
    queryFn: ({ signal }) => recommendationApi.catalog(signal),
  })
  const total = history.data?.pages[0].total
  const entries = history.data?.pages.flatMap((page) => page.items) ?? []
  const latest = entries.at(-1)
  return (
    <>
      <div className="page-heading dashboard-heading">
        <div>
          <span className="eyebrow">ТВОЙ ДЕНЬ. ТВОИ ОТКРЫТИЯ.</span>
          <h1>
            Как ты себя чувствуешь
            <br /> <span className="gradient-text">сегодня?</span>
          </h1>
          <p>Остановись на минуту. Мы найдём то, что откликнется.</p>
        </div>
        <div className="date-chip">
          <span className="status-dot" />
          {new Intl.DateTimeFormat('ru', {
            day: 'numeric',
            month: 'long',
            weekday: 'short',
          }).format(new Date())}
        </div>
      </div>
      <div className="dashboard-grid">
        <div>
          <MoodScanner initialText={example} />
          <div className="prompt-section">
            <span className="subtle-label">НЕ ЗНАЕШЬ, С ЧЕГО НАЧАТЬ?</span>
            <div>
              {prompts.map((text, i) => (
                <button
                  key={text}
                  onClick={() => {
                    setExample(text)
                    document.getElementById('mood-text')?.focus()
                  }}
                >
                  {['🌙', '✨', '☀️'][i]}{' '}
                  {['Замедлиться после учёбы', 'Найти вдохновение', 'Поймать хороший день'][i]}
                  <ArrowRight size={14} />
                </button>
              ))}
            </div>
          </div>
        </div>
        <aside className="context-column">
          <div className="context-card">
            <div className="section-heading">
              <span className="eyebrow">НАСТРОЙСЯ НА СЕБЯ</span>
              <Sparkles size={16} />
            </div>
            <Orb small />
            <h2>
              Каждое настроение —<br />
              начало чего-то нового.
            </h2>
            <p>
              Расскажи, что чувствуешь.
              <br />
              Остальное мы поможем найти.
            </p>
            <div className="context-pills">
              <span>
                <Headphones size={12} /> Музыка
              </span>
              <span>
                <Film size={12} /> Кино
              </span>
              <span>
                <BookOpen size={12} /> Книги
              </span>
            </div>
          </div>
          <div className="panel daily-card">
            <div className="section-heading">
              <h3>Твой контекст</h3>
              <span className="soft-icon">
                <Leaf size={16} />
              </span>
            </div>
            {history.isPending ? (
              <p className="muted">Загружаем историю…</p>
            ) : history.isError ? (
              <p className="muted">История пока недоступна.</p>
            ) : latest ? (
              <>
                <div className="last-mood">
                  <span>{moods[latest.analysis.mood].emoji}</span>
                  <div>
                    <strong>{moods[latest.analysis.mood].label}</strong>
                    <small>Последний загруженный чек-ин</small>
                  </div>
                  <span>
                    {percent(latest.analysis.energy)}
                    <small>энергия</small>
                  </span>
                </div>
                <Link to="/history">
                  {total} чек-ин · открыть историю <ArrowRight size={14} />
                </Link>
              </>
            ) : (
              <>
                <p>Здесь появится история твоих настроений.</p>
                <span className="subtle-label">ПЕРВЫЙ ШАГ — ПРОСТО БЫТЬ СОБОЙ</span>
              </>
            )}
          </div>
          <Link className="physics-link" to="/physics">
            <span className="soft-icon">
              <AudioLines size={22} />
            </span>
            <div>
              <strong>Как звучит твой мир?</strong>
              <small>Исследуй физику голоса</small>
            </div>
            <ArrowRight size={17} />
          </Link>
        </aside>
      </div>
      <section className="discovery-section">
        <div className="section-heading">
          <div>
            <span className="eyebrow">МАЛЕНЬКИЕ ОТКРЫТИЯ</span>
            <h2>А что, если сегодня…</h2>
          </div>
          <span className="subtle-label">ВЫБЕРИ НАПРАВЛЕНИЕ</span>
        </div>
        <div className="discovery-grid">
          {[
            {
              title: 'Раствориться в музыке',
              sub: 'Звук, который обнимает',
              kind: 'music',
              icon: Headphones,
              prompt: 'Хочу послушать спокойную атмосферную музыку',
            },
            {
              title: 'Сменить декорации',
              sub: 'Одна история — другой мир',
              kind: 'movie',
              icon: Film,
              prompt: 'Хочется посмотреть вдохновляющий фильм',
            },
            {
              title: 'Побыть в моменте',
              sub: 'Пауза тоже часть пути',
              kind: 'activity',
              icon: Leaf,
              prompt: 'Хочу отдохнуть на природе и немного погулять',
            },
          ].map(({ title, sub, kind, icon: Icon, prompt }) => (
            <button
              className={`discovery-card art-${kind}`}
              key={kind}
              onClick={() => {
                setExample(prompt)
                document.getElementById('mood-text')?.focus()
                window.scrollTo({ top: 0, behavior: 'smooth' })
              }}
            >
              <span className="discovery-art">
                <Icon size={48} strokeWidth={1} />
              </span>
              <span>
                <strong>{title}</strong>
                <small>{sub}</small>
              </span>
              <ArrowRight size={18} />
            </button>
          ))}
        </div>
        <p className="field-hint">
          Направления для нового запроса.{' '}
          {catalogue.data
            ? `В каталоге Backend — ${catalogue.data.length} материалов и занятий.`
            : 'Подбор появится после чек-ина.'}
        </p>
      </section>
    </>
  )
}
