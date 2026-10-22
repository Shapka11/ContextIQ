import { useState } from 'react'
import {
  BookOpen,
  ExternalLink,
  Film,
  Headphones,
  Leaf,
  Sparkles,
  ThumbsDown,
  ThumbsUp,
} from 'lucide-react'
import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { feedbackApi } from '../../api'
import { categories, percent, safeUrl, tagLabel } from '../../lib/utils'
import type { Recommendation } from '../../types/api'
import { Button } from '../ui/button'
import { ErrorState } from '../ui/states'
import { RecommendationExplanation } from './RecommendationExplanation'
const icons = { movie: Film, music: Headphones, book: BookOpen, activity: Leaf }
export function RecommendationCard({
  recommendation,
  index,
}: {
  recommendation: Recommendation
  index: number
}) {
  const { item } = recommendation
  const [rating, setRating] = useState<1 | -1 | null>(null)
  const Icon = icons[item.category]
  const url = safeUrl(item.externalUrl)
  const feedback = useMutation({
    mutationFn: (value: 1 | -1) =>
      feedbackApi.save({ recommendationId: recommendation.id, rating: value }),
    onSuccess: (response) => {
      setRating(response.rating)
      toast.success('Спасибо! Оценка сохранена.')
    },
  })
  return (
    <article className="recommendation-card panel" style={{ animationDelay: `${index * 60}ms` }}>
      <div
        className={`cover-art art-${item.category}`}
        aria-label="Тематическая иллюстрация, не обложка произведения"
      >
        <span className="cover-category">{categories[item.category]}</span>
        <div className="cover-orbit" />
        <Icon size={76} strokeWidth={0.8} />
        <span className="cover-number">{String(index + 1).padStart(2, '0')}</span>
        <span className="cover-caption">В ТВОЁМ КОНТЕКСТЕ</span>
      </div>
      <div className="recommendation-body">
        <div className="section-heading">
          <span className="eyebrow">{categories[item.category]}</span>
          <span className="match-score">
            <Sparkles size={12} />
            {percent(recommendation.score)} совпадение
          </span>
        </div>
        <h3>{item.title}</h3>
        <div className="tags">
          {item.tags.map((tag) => (
            <span key={tag}>{tagLabel(tag)}</span>
          ))}
        </div>
        <RecommendationExplanation text={recommendation.explanation} />
        <div className="recommendation-actions">
          <div>
            {([1, -1] as const).map((value, i) => (
              <button
                key={value}
                aria-label={i === 0 ? `Нравится: ${item.title}` : `Не нравится: ${item.title}`}
                aria-pressed={rating === value}
                className={`icon-button ${rating === value ? 'selected' : ''}`}
                disabled={feedback.isPending}
                onClick={() => feedback.mutate(value)}
              >
                {i === 0 ? <ThumbsUp size={17} /> : <ThumbsDown size={17} />}
              </button>
            ))}
          </div>
          {url && (
            <Button asChild variant="ghost" size="sm">
              <a href={url} target="_blank" rel="noopener noreferrer">
                Открыть <ExternalLink size={14} />
              </a>
            </Button>
          )}
        </div>
        {feedback.isError && <ErrorState error={feedback.error} />}
      </div>
    </article>
  )
}
