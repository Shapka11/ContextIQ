import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { ArrowRight, Sparkles, Zap } from 'lucide-react'
import type { Category, CheckInResponse, Goal } from '../types/api'
import { categories, moods, percent, tagLabel } from '../lib/utils'
import { RecommendationCard } from '../components/recommendations/RecommendationCard'
import { Button } from '../components/ui/button'
import { EmptyState } from '../components/ui/states'
export default function ResultsPage() {
  const client = useQueryClient()
  const result = client.getQueryData<CheckInResponse>(['result'])
  const goal = client.getQueryData<Goal>(['goal'])
  const [category, setCategory] = useState<Category | 'all'>('all')
  if (!result)
    return (
      <EmptyState
        title="Твой вайб ещё впереди"
        text="Расскажи о своём настроении, и здесь появится персональная подборка. Результат текущего сеанса не сохраняется в браузере после перезагрузки."
      >
        <Button asChild>
          <Link to="/dashboard">
            К сканеру настроения <ArrowRight size={16} />
          </Link>
        </Button>
      </EmptyState>
    )
  const { moodEntry, recommendations } = result
  const { analysis } = moodEntry
  const mood = moods[analysis.mood]
  return (
    <>
      <div className="page-heading">
        <span className="eyebrow">СОБРАНО В ТВОЁМ КОНТЕКСТЕ</span>
        <h1>
          Кажется, это <span className="gradient-text">твой вайб.</span>
        </h1>
        <p>Маленькие открытия, которые могут откликнуться прямо сейчас.</p>
      </div>
      <section className="vibe-banner">
        <div className="vibe-mood-emoji">{mood.emoji}</div>
        <div className="vibe-summary">
          <span className="eyebrow">
            <Sparkles size={13} /> ПРЕДПОЛАГАЕМОЕ НАСТРОЕНИЕ
          </span>
          <h2>{mood.label}</h2>
          <p>
            Анализ текста и эмодзи выделил темы:{' '}
            {analysis.themes.length ? analysis.themes.map(tagLabel).join(', ') : 'не обнаружены'}.
            Уверенность алгоритма — {percent(analysis.confidence)}, это не клиническая оценка.
          </p>
          <div className="tags">
            {analysis.themes.map((theme) => (
              <span key={theme}>{tagLabel(theme)}</span>
            ))}
          </div>
        </div>
        <div className="vibe-stats">
          <span>
            <Zap size={16} /> Энергия <strong>{percent(analysis.energy)}</strong>
          </span>
          <div className="energy-track">
            <i style={{ width: percent(analysis.energy) }} />
          </div>
          <span>
            Твой ориентир{' '}
            <strong>{goal === 'change' ? 'Изменить настроение' : 'Поддержать настроение'}</strong>
          </span>
          <small>Ориентир не участвует в ранжировании API.</small>
        </div>
      </section>
      {moodEntry.analysisMode === 'demo' && (
        <div className="mode-note">
          <Sparkles size={15} />{' '}
          {import.meta.env.VITE_DEMO_MODE === 'true'
            ? 'Тестовый сценарий Frontend.'
            : 'AI-сервис работает в режиме demo: детерминированные правила, без LLM.'}
        </div>
      )}
      <div className="results-toolbar">
        <div className="filter-tabs" aria-label="Категории рекомендаций">
          {(['all', ...Object.keys(categories)] as (Category | 'all')[]).map((value) => (
            <button
              key={value}
              aria-pressed={category === value}
              className={category === value ? 'active' : ''}
              onClick={() => setCategory(value)}
            >
              {value === 'all' ? 'Всё для тебя' : categories[value]}
            </button>
          ))}
        </div>
        <span className="subtle-label">
          {recommendations.filter((r) => category === 'all' || r.item.category === category).length}{' '}
          В ПОДБОРКЕ
        </span>
      </div>
      <div className="recommendation-grid">
        {recommendations
          .filter((r) => category === 'all' || r.item.category === category)
          .map((recommendation, i) => (
            <RecommendationCard key={recommendation.id} recommendation={recommendation} index={i} />
          ))}
      </div>
      {!recommendations.some((r) => category === 'all' || r.item.category === category) && (
        <EmptyState
          title="В этой категории пока тихо"
          text="В ответе сервиса нет рекомендаций этой категории. Посмотри остальные варианты."
        />
      )}
      <p className="field-hint">
        Совпадение — score алгоритма подбора, а не рейтинг произведения. Иллюстрации тематические.
      </p>
      <div className="result-bottom">
        <p>Другой момент — другой контекст.</p>
        <Button asChild variant="secondary">
          <Link to="/dashboard">
            Новый чек-ин <ArrowRight size={16} />
          </Link>
        </Button>
      </div>
    </>
  )
}
