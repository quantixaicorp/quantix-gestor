import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

beforeEach(() => {
  vi.resetModules()
  localStorage.clear()
  mockFetch.mockReset()
})

describe('api', () => {
  it('envia Authorization header com token do localStorage', async () => {
    localStorage.setItem('ga_token', 'meu-token')
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    })

    const { api } = await import('@/services/api')
    await api.get('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer meu-token' }),
      })
    )
  })

  it('em 401 tenta refresh e retenta request', async () => {
    localStorage.setItem('ga_token', 'token-expirado')
    localStorage.setItem('ga_refresh_token', 'refresh-valido')

    mockFetch
      .mockResolvedValueOnce({ ok: false, status: 401 })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ access_token: 'novo-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'ok' }),
      })

    const { api } = await import('@/services/api')
    const result = await api.get('/protegido')

    expect(result).toEqual({ data: 'ok' })
    expect(localStorage.getItem('ga_token')).toBe('novo-token')
  })

  it('em falha do refresh limpa tokens e redireciona para /auth', async () => {
    localStorage.setItem('ga_token', 'token-expirado')
    localStorage.setItem('ga_refresh_token', 'refresh-invalido')

    // Mock location.href assignment
    const locationMock = { href: '' }
    vi.stubGlobal('location', locationMock)

    mockFetch
      .mockResolvedValueOnce({ ok: false, status: 401 })
      .mockResolvedValueOnce({ ok: false, status: 400 })

    const { api } = await import('@/services/api')
    await expect(api.get('/protegido')).rejects.toThrow('Unauthorized')

    expect(localStorage.getItem('ga_token')).toBeNull()
    expect(localStorage.getItem('ga_refresh_token')).toBeNull()
    expect(locationMock.href).toBe('/auth')
  })
})
