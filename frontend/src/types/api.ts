export type Category = 'movie' | 'music' | 'book' | 'activity'
export type Mood = 'happy' | 'sad' | 'stressed' | 'excited' | 'neutral'
export interface GuestSession {
  userId: string
  token: string
  expiresAt: string
}
export interface Profile {
  userId: string
  favoriteGenres: string[]
  dislikedGenres: string[]
  favoriteCategories: Category[]
  updatedAt: string
}
export type ProfilePatch = Partial<
  Pick<Profile, 'favoriteGenres' | 'dislikedGenres' | 'favoriteCategories'>
>
export interface CheckInRequest {
  text: string
  emoji: string
}
export interface Analysis {
  mood: Mood
  energy: number
  valence: number
  themes: string[]
  confidence: number
}
export interface MoodEntry {
  id: string
  text: string
  emoji: string
  analysis: Analysis
  analysisMode: string
  createdAt: string
}
export interface CatalogItem {
  id: string
  sourceId: string
  title: string
  category: Category
  tags: string[]
  energy: number
  moodTags: Mood[]
  externalUrl: string | null
}
export interface Recommendation {
  id: string
  item: CatalogItem
  score: number
  explanation: string
}
export interface CheckInResponse {
  moodEntry: MoodEntry
  recommendations: Recommendation[]
}
export interface HistoryResponse {
  page: number
  pageSize: number
  total: number
  items: MoodEntry[]
}
export interface FeedbackRequest {
  recommendationId: string
  rating: 1 | -1
}
export interface FeedbackResponse extends FeedbackRequest {
  id: string
  createdAt: string
}
export interface AudioFeatures {
  durationSeconds: number
  sampleRate: number
  rms: number
  peakAmplitude: number
  dominantFrequencyHz: number
  spectralCentroidHz: number
}
export interface Health {
  status: string
  database: string
  python: { status: string; textAnalysisMode: string; version: string } | null
}
export interface ProblemDetails {
  title?: string
  detail?: string
  status?: number
  traceId?: string
}
export type Goal = 'support' | 'change'
