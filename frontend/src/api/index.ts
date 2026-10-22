import { DEMO_MODE } from '../lib/session'
import type {
  AudioFeatures,
  CatalogItem,
  CheckInRequest,
  CheckInResponse,
  FeedbackRequest,
  FeedbackResponse,
  GuestSession,
  Health,
  HistoryResponse,
  Profile,
  ProfilePatch,
} from '../types/api'
import { demo } from './demo'
import { http } from './http'
export const sessionApi = {
  create: () =>
    DEMO_MODE ? demo.session() : http<GuestSession>('/api/sessions/guest', { method: 'POST' }),
}
export const profileApi = {
  get: (signal?: AbortSignal) =>
    DEMO_MODE ? demo.profile() : http<Profile>('/api/profile', { signal }),
  patch: (input: ProfilePatch) =>
    DEMO_MODE
      ? demo.patch(input)
      : http<Profile>('/api/profile', { method: 'PATCH', body: JSON.stringify(input) }),
  delete: () => (DEMO_MODE ? demo.delete() : http<void>('/api/profile', { method: 'DELETE' })),
}
export const moodApi = {
  checkin: (input: CheckInRequest, signal?: AbortSignal) =>
    DEMO_MODE
      ? demo.checkin(input)
      : http<CheckInResponse>('/api/checkins', {
          method: 'POST',
          body: JSON.stringify(input),
          signal,
        }),
}
export const audioApi = {
  analyze: (file: Blob, signal?: AbortSignal) => {
    const body = new FormData()
    body.append('file', file, 'recording.wav')
    return DEMO_MODE
      ? demo.audio()
      : http<AudioFeatures>('/api/audio/analyze', { method: 'POST', body, signal })
  },
}
export const recommendationApi = {
  catalog: (signal?: AbortSignal) =>
    DEMO_MODE ? demo.catalog() : http<CatalogItem[]>('/api/catalog', { signal }),
}
export const feedbackApi = {
  save: (input: FeedbackRequest) =>
    DEMO_MODE
      ? demo.feedback(input)
      : http<FeedbackResponse>('/api/feedback', { method: 'POST', body: JSON.stringify(input) }),
}
export const historyApi = {
  page: (page = 1, pageSize = 100, signal?: AbortSignal) =>
    DEMO_MODE
      ? demo.history(page, pageSize)
      : http<HistoryResponse>(`/api/history?page=${page}&pageSize=${pageSize}`, { signal }),
}
export const healthApi = {
  get: (signal?: AbortSignal) =>
    DEMO_MODE
      ? Promise.resolve<Health>({ status: 'ok', database: 'demo', python: null })
      : http<Health>('/api/health', { signal }),
}
