import { sessionStore } from '../lib/session'
import type { ProblemDetails } from '../types/api'
export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public traceId?: string,
  ) {
    super(message)
  }
}
const messages: Record<number, string> = {
  400: 'Проверьте введённые данные.',
  401: 'Гостевая сессия завершилась. Начните новую на стартовой странице.',
  403: 'Нет доступа к этим данным.',
  404: 'Данные не найдены.',
  413: 'Запись слишком большая. Максимум 20 МиБ.',
  422: 'Не удалось прочитать запись. Нужен корректный PCM WAV.',
  429: 'Слишком много запросов. Попробуйте через минуту.',
  502: 'Сервис анализа вернул ошибку. Попробуйте позже.',
  503: 'Сервис анализа временно недоступен. Проверьте запуск Backend и AI-сервиса.',
}
export async function http<T>(path: string, options: RequestInit = {}): Promise<T> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), 65_000)
  const abort = () => controller.abort()
  options.signal?.addEventListener('abort', abort, { once: true })
  if (options.signal?.aborted) controller.abort()
  const headers = new Headers(options.headers)
  const session = sessionStore.get()
  if (session) headers.set('Authorization', `Bearer ${session.token}`)
  if (options.body && !(options.body instanceof FormData))
    headers.set('Content-Type', 'application/json')
  try {
    const response = await fetch(
      `${(import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000').replace(/\/$/, '')}${path}`,
      { ...options, headers, signal: controller.signal },
    )
    if (!response.ok) {
      const problem = (await response.json().catch(() => ({}))) as ProblemDetails
      if (response.status === 401) sessionStore.clear()
      throw new ApiError(
        response.status,
        messages[response.status] ?? 'Не удалось выполнить запрос. Попробуйте ещё раз.',
        problem.traceId,
      )
    }
    if (response.status === 204) return undefined as T
    return (await response.json()) as T
  } catch (error) {
    if (error instanceof ApiError) throw error
    if (controller.signal.aborted)
      throw new ApiError(
        0,
        options.signal?.aborted ? 'Запрос отменён.' : 'Время ожидания истекло. Попробуйте ещё раз.',
      )
    throw new ApiError(0, 'Нет соединения с Backend. Проверьте адрес API и запуск сервера.')
  } finally {
    clearTimeout(timer)
    options.signal?.removeEventListener('abort', abort)
  }
}
