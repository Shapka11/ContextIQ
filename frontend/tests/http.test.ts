import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { sessionStore } from '../src/lib/session'
import { ApiError, http } from '../src/api/http'

describe('HTTP transport', () => {
  beforeEach(() => {
    vi.spyOn(sessionStore, 'get').mockReturnValue({ userId: 'test', token: 'test-token', expiresAt: '2099-01-01' })
    vi.spyOn(sessionStore, 'clear').mockImplementation(() => {})
    vi.stubEnv('VITE_API_BASE_URL', '')
  })
  afterEach(() => { vi.restoreAllMocks(); vi.unstubAllGlobals(); vi.unstubAllEnvs() })

  it('uses same-origin API and Bearer JSON without a localhost fallback', async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json({ ok: true }))
    vi.stubGlobal('fetch', fetch)
    expect(await http('/api/checkins', { method: 'POST', body: JSON.stringify({ text: 'hello' }) })).toEqual({ ok: true })
    const [path, options] = fetch.mock.calls[0]
    expect(path).toBe('/api/checkins')
    expect(options.headers.get('Authorization')).toBe('Bearer test-token')
    expect(options.headers.get('Content-Type')).toBe('application/json')
  })

  it('lets the browser set the multipart boundary for WAV', async () => {
    const fetch = vi.fn().mockResolvedValue(Response.json({ rms: 0.35 }))
    vi.stubGlobal('fetch', fetch)
    const body = new FormData()
    body.append('file', new Blob(['RIFF']), 'test.wav')
    await http('/api/audio/analyze', { method: 'POST', body })
    expect(fetch.mock.calls[0][1].headers.has('Content-Type')).toBe(false)
  })

  it('clears expired sessions and preserves safe trace identifiers', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(Response.json({ traceId: 'trace-1', detail: 'private' }, { status: 401 })))
    await expect(http('/api/profile')).rejects.toMatchObject({ status: 401, traceId: 'trace-1' })
    expect(sessionStore.clear).toHaveBeenCalledOnce()
  })

  it('maps service unavailability and network errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('', { status: 503 })))
    await expect(http('/api/checkins')).rejects.toMatchObject({ status: 503 })
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')))
    await expect(http('/api/health')).rejects.toBeInstanceOf(ApiError)
    await expect(http('/api/health')).rejects.toMatchObject({ status: 0 })
  })

  it('supports empty 204 responses', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(null, { status: 204 })))
    expect(await http('/api/profile', { method: 'DELETE' })).toBeUndefined()
  })
})
