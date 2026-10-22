import type {
  AudioFeatures,
  CatalogItem,
  CheckInRequest,
  CheckInResponse,
  FeedbackRequest,
  FeedbackResponse,
  GuestSession,
  HistoryResponse,
  MoodEntry,
  Profile,
  ProfilePatch,
} from '../types/api'
const stamp = '2026-10-22T12:00:00Z'
const catalog: CatalogItem[] = [
  {
    id: 'demo-music',
    sourceId: 'music-08',
    title: 'Brian Eno — An Ending (Ascent)',
    category: 'music',
    tags: ['ambient', 'relax'],
    energy: 0.1,
    moodTags: ['neutral'],
    externalUrl: null,
  },
  {
    id: 'demo-movie',
    sourceId: 'movie-03',
    title: 'Мой сосед Тоторо',
    category: 'movie',
    tags: ['nature', 'animation'],
    energy: 0.2,
    moodTags: ['neutral'],
    externalUrl: null,
  },
  {
    id: 'demo-book',
    sourceId: 'book-03',
    title: 'Рэй Брэдбери — Вино из одуванчиков',
    category: 'book',
    tags: ['reading', 'nature'],
    energy: 0.2,
    moodTags: ['neutral'],
    externalUrl: null,
  },
  {
    id: 'demo-activity',
    sourceId: 'activity-01',
    title: 'Спокойная прогулка в парке',
    category: 'activity',
    tags: ['walking', 'nature'],
    energy: 0.25,
    moodTags: ['neutral'],
    externalUrl: null,
  },
]
const initial: Profile = {
  userId: 'demo-user',
  favoriteGenres: ['ambient', 'nature'],
  dislikedGenres: [],
  favoriteCategories: ['music', 'movie'],
  updatedAt: stamp,
}
let profile = { ...initial }
let entries: MoodEntry[] = []
export const demo = {
  session: async (): Promise<GuestSession> => ({
    userId: 'demo-user',
    token: 'demo-only',
    expiresAt: '2099-01-01T00:00:00Z',
  }),
  profile: async () => ({ ...profile }),
  patch: async (patch: ProfilePatch) => (profile = { ...profile, ...patch, updatedAt: stamp }),
  delete: async () => {
    profile = { ...initial }
    entries = []
  },
  catalog: async () => catalog,
  history: async (page: number, pageSize: number): Promise<HistoryResponse> => ({
    page,
    pageSize,
    total: entries.length,
    items: entries.slice((page - 1) * pageSize, page * pageSize),
  }),
  checkin: async (input: CheckInRequest): Promise<CheckInResponse> => {
    const energetic = input.emoji === '🥳' || input.emoji === '😊'
    const moodEntry: MoodEntry = {
      id: `demo-${entries.length + 1}`,
      ...input,
      analysis: {
        mood: energetic ? 'happy' : 'neutral',
        energy: energetic ? 0.8 : 0.3,
        valence: energetic ? 0.7 : 0.1,
        themes: ['relax', 'nature'],
        confidence: 0.7,
      },
      analysisMode: 'demo',
      createdAt: stamp,
    }
    entries = [...entries, moodEntry]
    return {
      moodEntry,
      recommendations: catalog.map((item, i) => ({
        id: `demo-rec-${i}`,
        item,
        score: 0.9 - i * 0.06,
        explanation: `Тестовый пример: ${item.tags.map((t) => (t === 'nature' ? 'природа' : 'спокойный отдых')).join(' и ')} перекликаются с темами фиксированного демо-сценария. Это не результат AI-анализа.`,
      })),
    }
  },
  feedback: async (input: FeedbackRequest): Promise<FeedbackResponse> => ({
    ...input,
    id: 'demo-feedback',
    createdAt: stamp,
  }),
  audio: async (): Promise<AudioFeatures> => ({
    durationSeconds: 2,
    sampleRate: 48000,
    rms: 0.35355,
    peakAmplitude: 0.5,
    dominantFrequencyHz: 440,
    spectralCentroidHz: 440,
  }),
}
