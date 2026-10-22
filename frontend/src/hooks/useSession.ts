import { useSyncExternalStore } from 'react'
import { sessionStore } from '../lib/session'
function subscribe(callback: () => void) {
  window.addEventListener('session-change', callback)
  window.addEventListener('storage', callback)
  return () => {
    window.removeEventListener('session-change', callback)
    window.removeEventListener('storage', callback)
  }
}
export function useSession() {
  return useSyncExternalStore(subscribe, () => sessionStore.get()?.token ?? null)
}
