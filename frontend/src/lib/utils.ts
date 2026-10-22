import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
export const categories = {
  movie: 'Фильмы',
  music: 'Музыка',
  book: 'Книги',
  activity: 'Активности',
} as const
export const moods = {
  happy: { label: 'Радостное', emoji: '😊' },
  sad: { label: 'Грустное', emoji: '😔' },
  stressed: { label: 'Напряжённое', emoji: '😤' },
  excited: { label: 'Воодушевлённое', emoji: '🥳' },
  neutral: { label: 'Спокойное', emoji: '😌' },
} as const
export const tagNames: Record<string, string> = {
  relax: 'Расслабление',
  ambient: 'Эмбиент',
  rock: 'Рок',
  jazz: 'Джаз',
  classical: 'Классика',
  animation: 'Анимация',
  adventure: 'Приключения',
  fantasy: 'Фэнтези',
  'science-fiction': 'Фантастика',
  comedy: 'Комедия',
  walking: 'Прогулки',
  drawing: 'Рисование',
  sport: 'Спорт',
  reading: 'Чтение',
  nature: 'Природа',
  space: 'Космос',
  science: 'Наука',
  social: 'Общение',
  photography: 'Фотография',
  music: 'Музыка',
  family: 'Семейное',
  'spider-man': 'Человек-паук',
}
export const tagLabel = (tag: string) => tagNames[tag] ?? tag
export const dateLabel = (date: string) =>
  new Intl.DateTimeFormat('ru', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
export const percent = (value: number) => `${Math.round(value * 100)}%`
export function safeUrl(value: string | null) {
  if (!value) return undefined
  try {
    const url = new URL(value)
    return ['https:', 'http:'].includes(url.protocol) ? url.href : undefined
  } catch {
    return undefined
  }
}
