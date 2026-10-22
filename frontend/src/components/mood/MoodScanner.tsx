import { useEffect, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { ArrowRight, LoaderCircle, Sparkles, X } from 'lucide-react'
import { toast } from 'sonner'
import { moodApi } from '../../api'
import type { CheckInRequest, Goal } from '../../types/api'
import { Button } from '../ui/button'
import { ErrorState } from '../ui/states'
import { AudioRecorder } from '../audio/AudioRecorder'
const emojis = [
  ['😊', 'Радостно'],
  ['😔', 'Грустно'],
  ['😴', 'Усталость'],
  ['😤', 'Раздражение'],
  ['😎', 'Уверенность'],
  ['🥳', 'Восторг'],
  ['😌', 'Спокойствие'],
  ['🤯', 'Перегрузка'],
]
export function MoodScanner({ initialText = '' }: { initialText?: string }) {
  const [text, setText] = useState(initialText)
  const [emoji, setEmoji] = useState('')
  const [goal, setGoal] = useState<Goal>('support')
  const [validation, setValidation] = useState('')
  const navigate = useNavigate()
  const client = useQueryClient()
  const controller = useRef<AbortController | null>(null)
  const previousExample = useRef(initialText)
  useEffect(() => {
    if (previousExample.current !== initialText) {
      previousExample.current = initialText
      setText(initialText)
    }
  }, [initialText])
  useEffect(() => () => controller.current?.abort(), [])
  const mutation = useMutation({
    mutationFn: (input: CheckInRequest) => {
      controller.current = new AbortController()
      return moodApi.checkin(input, controller.current.signal)
    },
    onSuccess: (response) => {
      client.setQueryData(['result'], response)
      client.setQueryData(['goal'], goal)
      void client.invalidateQueries({ queryKey: ['history'] })
      navigate('/results')
    },
  })
  function submit(event: React.FormEvent) {
    event.preventDefault()
    if (!text.trim() && !emoji) {
      setValidation('Расскажите о настроении или выберите эмодзи.')
      toast.error('Добавьте текст или эмодзи')
      return
    }
    setValidation('')
    mutation.mutate({ text: text.trim(), emoji })
  }
  return (
    <form onSubmit={submit} className="scanner panel">
      <div className="section-heading">
        <span className="eyebrow">
          <span className="status-dot" /> МОМЕНТ ДЛЯ СЕБЯ
        </span>
        <span className="subtle-label">01 / ЧЕК-ИН</span>
      </div>
      <label htmlFor="mood-text" className="scanner-label">
        Что сейчас у тебя на душе?
      </label>
      <div className="textarea-wrap">
        <textarea
          id="mood-text"
          placeholder="Можно честно. Устал, вдохновлён или просто хочется чего-то нового…"
          value={text}
          onChange={(e) => setText(e.target.value)}
          maxLength={8000}
          disabled={mutation.isPending}
          aria-invalid={!!validation}
          aria-describedby="mood-help"
        />
        <div className="textarea-footer">
          <span>
            <Sparkles size={13} /> Здесь можно быть собой
          </span>
          <span>{text.length} / 8000</span>
        </div>
      </div>
      <div className="emoji-heading">
        <span>Или выбери настроение</span>
        <span className="muted">Одним касанием</span>
      </div>
      <div className="emoji-grid">
        {emojis.map(([value, label]) => (
          <button
            key={value}
            type="button"
            title={label}
            aria-label={label}
            aria-pressed={emoji === value}
            disabled={mutation.isPending}
            className={`emoji-button ${emoji === value ? 'selected' : ''}`}
            onClick={() => setEmoji(emoji === value ? '' : value)}
          >
            {value}
          </button>
        ))}
      </div>
      <fieldset className="goal-switch">
        <legend>Чего тебе хочется?</legend>
        <div>
          {(['support', 'change'] as const).map((value, i) => (
            <button
              key={value}
              type="button"
              className={goal === value ? 'selected' : ''}
              aria-pressed={goal === value}
              onClick={() => setGoal(value)}
            >
              {i === 0 ? '☀' : '↗'} {i === 0 ? 'Поддержать настроение' : 'Изменить настроение'}
            </button>
          ))}
        </div>
      </fieldset>
      <p className="field-hint" id="mood-help">
        Цель — твой ориентир на этом экране. Текущая версия анализа её не учитывает.
      </p>
      <AudioRecorder />
      {validation && (
        <p className="validation" role="alert">
          {validation}
        </p>
      )}
      {mutation.isError && <ErrorState error={mutation.error} />}
      <Button className="find-vibe" disabled={mutation.isPending}>
        {mutation.isPending ? (
          <>
            <LoaderCircle className="spin" size={19} /> Анализируем твой контекст…
          </>
        ) : (
          <>
            <Sparkles size={19} /> Найти мой вайб <ArrowRight size={19} />
          </>
        )}
      </Button>
      {mutation.isPending && (
        <div className="analysis-progress" role="status">
          <span /> Анализ настроения → подбор контента{' '}
          <button
            type="button"
            onClick={() => controller.current?.abort()}
            aria-label="Отменить анализ"
          >
            <X size={15} />
          </button>
        </div>
      )}
      <p className="scanner-privacy">
        Сохраняются только отправленные текст и эмодзи. Это предположение о настроении, не диагноз.
      </p>
    </form>
  )
}
