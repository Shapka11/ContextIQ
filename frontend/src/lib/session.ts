import type { GuestSession } from '../types/api'
export const DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true'
const key = DEMO_MODE ? 'contextiq.demo.session' : 'contextiq.session'
export const sessionStore = {
  get(): GuestSession | null {
    try {
      const raw: unknown = JSON.parse(localStorage.getItem(key) ?? 'null')
      if (
        raw &&
        typeof raw === 'object' &&
        'token' in raw &&
        typeof raw.token === 'string' &&
        'userId' in raw &&
        typeof raw.userId === 'string' &&
        'expiresAt' in raw &&
        typeof raw.expiresAt === 'string' &&
        Date.parse(raw.expiresAt) > Date.now()
      )
        return raw as GuestSession
    } catch {
    }
    return null
  },
  set(value: GuestSession) {
    localStorage.setItem(key, JSON.stringify(value))
    window.dispatchEvent(new Event('session-change'))
  },
  clear() {
    localStorage.removeItem(key)
    window.dispatchEvent(new Event('session-change'))
  },
}
