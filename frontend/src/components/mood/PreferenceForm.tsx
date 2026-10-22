import { useState } from 'react'
import { Check, Plus, Save } from 'lucide-react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { profileApi } from '../../api'
import type { Category, Profile } from '../../types/api'
import { categories, tagLabel } from '../../lib/utils'
import { Button } from '../ui/button'
import { ErrorState } from '../ui/states'
const groups = [
  {
    title: 'Музыка, которая откликается',
    caption: '01 / МУЗЫКА',
    tags: ['ambient', 'rock', 'jazz', 'classical'],
  },
  {
    title: 'Истории, в которые хочется погрузиться',
    caption: '02 / КИНО И КНИГИ',
    tags: ['animation', 'adventure', 'fantasy', 'science-fiction', 'comedy'],
  },
  {
    title: 'Чем любишь заниматься?',
    caption: '03 / АКТИВНОСТИ',
    tags: ['walking', 'drawing', 'sport', 'reading', 'photography', 'social'],
  },
  {
    title: 'Что тебя вдохновляет?',
    caption: '04 / ИНТЕРЕСЫ',
    tags: ['nature', 'space', 'science', 'spider-man', 'relax'],
  },
]
export function PreferenceForm({
  profile,
  onSaved,
  onboarding = false,
}: {
  profile?: Profile
  onSaved?: () => void
  onboarding?: boolean
}) {
  const [favorite, setFavorite] = useState(profile?.favoriteGenres ?? [])
  const [disliked, setDisliked] = useState(profile?.dislikedGenres ?? [])
  const [selectedCategories, setCategories] = useState<Category[]>(
    profile?.favoriteCategories ?? [],
  )
  const [custom, setCustom] = useState('')
  const client = useQueryClient()
  const save = useMutation({
    mutationFn: () =>
      profileApi.patch({
        favoriteGenres: favorite,
        dislikedGenres: disliked,
        favoriteCategories: selectedCategories,
      }),
    onSuccess: (response) => {
      client.setQueryData(['profile'], response)
      toast.success('Предпочтения сохранены')
      onSaved?.()
    },
  })
  function toggle(tag: string, negative = false) {
    const list = negative ? disliked : favorite
    if (!list.includes(tag) && list.length >= 50) {
      toast.error('Можно выбрать не больше 50 тегов в каждом списке.')
      return
    }
    if (negative) {
      setDisliked((current) =>
        current.includes(tag) ? current.filter((t) => t !== tag) : [...current, tag],
      )
      setFavorite((current) => current.filter((t) => t !== tag))
    } else {
      setFavorite((current) =>
        current.includes(tag) ? current.filter((t) => t !== tag) : [...current, tag],
      )
      setDisliked((current) => current.filter((t) => t !== tag))
    }
  }
  function addCustom() {
    const tag = custom.trim().toLowerCase()
    if (!tag || tag.length > 100 || favorite.length >= 50) return
    if (!favorite.includes(tag)) toggle(tag)
    setCustom('')
  }
  const known = groups.flatMap((g) => g.tags)
  return (
    <form
      className="preferences"
      onSubmit={(e) => {
        e.preventDefault()
        save.mutate()
      }}
    >
      <div className="preference-grid">
        {groups.map((group) => (
          <section className="panel preference-group" key={group.caption}>
            <span className="eyebrow">{group.caption}</span>
            <h3>{group.title}</h3>
            <div className="chips">
              {group.tags.map((tag) => (
                <button
                  type="button"
                  className={`chip ${favorite.includes(tag) ? 'selected' : ''}`}
                  aria-pressed={favorite.includes(tag)}
                  key={tag}
                  onClick={() => toggle(tag)}
                >
                  {favorite.includes(tag) && <Check size={13} />}
                  {tagLabel(tag)}
                </button>
              ))}
            </div>
          </section>
        ))}
      </div>
      <section className="panel preference-group">
        <h3>Твои интересы своими словами</h3>
        <div className="input-row">
          <label className="sr-only" htmlFor="custom-tag">
            Добавить интерес
          </label>
          <input
            id="custom-tag"
            placeholder="Например, архитектура"
            value={custom}
            maxLength={100}
            onChange={(e) => setCustom(e.target.value)}
          />
          <Button
            type="button"
            variant="secondary"
            onClick={addCustom}
            aria-label="Добавить интерес"
          >
            <Plus size={18} />
          </Button>
        </div>
        <div className="chips">
          {favorite
            .filter((t) => !known.includes(t))
            .map((tag) => (
              <button type="button" key={tag} className="chip selected" onClick={() => toggle(tag)}>
                {tagLabel(tag)} ×
              </button>
            ))}
        </div>
        <p className="field-hint">
          Жанры, активности и интересы хранятся в общем списке тегов предпочтений. До 50 тегов.
        </p>
      </section>
      <section className="panel preference-group">
        <span className="eyebrow">05 / БЕЗ ЭТОГО</span>
        <h3>Что лучше не предлагать?</h3>
        <div className="chips">
          {Array.from(new Set([...known, ...disliked])).map((tag) => (
            <button
              type="button"
              className={`chip ${disliked.includes(tag) ? 'negative' : ''}`}
              aria-pressed={disliked.includes(tag)}
              key={tag}
              onClick={() => toggle(tag, true)}
            >
              {tagLabel(tag)}
            </button>
          ))}
        </div>
      </section>
      <section className="panel preference-group">
        <h3>В каких категориях искать?</h3>
        <div className="chips">
          {(Object.keys(categories) as Category[]).map((category) => (
            <button
              type="button"
              className={`chip ${selectedCategories.includes(category) ? 'selected' : ''}`}
              aria-pressed={selectedCategories.includes(category)}
              key={category}
              onClick={() =>
                setCategories((current) =>
                  current.includes(category)
                    ? current.filter((c) => c !== category)
                    : [...current, category],
                )
              }
            >
              {categories[category]}
            </button>
          ))}
        </div>
      </section>
      {save.isError && <ErrorState error={save.error} />}
      <div className="preference-actions">
        <p className="muted">Можно изменить в любой момент в профиле.</p>
        <Button disabled={save.isPending}>
          <Save size={16} />
          {save.isPending
            ? 'Сохраняем…'
            : onboarding
              ? 'Всё готово, начнём'
              : 'Сохранить настройки'}
        </Button>
      </div>
    </form>
  )
}
