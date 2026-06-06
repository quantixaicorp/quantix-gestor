const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'
const ADMIN_URL = import.meta.env.VITE_ADMIN_URL ?? 'http://localhost:5001'
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID ?? 'gestorai'

async function refreshAccessToken(): Promise<string | null> {
  const refresh = localStorage.getItem('ga_refresh_token')
  if (!refresh) return null

  const res = await fetch(`${ADMIN_URL}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: CLIENT_ID,
      refresh_token: refresh,
    }),
  })

  if (!res.ok) {
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    window.location.href = '/auth'
    return null
  }

  const data = await res.json() as { access_token: string; refresh_token?: string }
  if (!data.access_token) {
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    window.location.href = '/auth'
    return null
  }
  if (data.refresh_token) {
    localStorage.setItem('ga_refresh_token', data.refresh_token)
  }
  localStorage.setItem('ga_token', data.access_token)
  return data.access_token
}

async function requestText(path: string): Promise<string> {
  const token = localStorage.getItem('ga_token')
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) throw new Error(res.statusText)
  return res.text()
}

async function request<T>(path: string, options: RequestInit = {}, retried = false): Promise<T> {
  const token = localStorage.getItem('ga_token')

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  })

  if (res.status === 401) {
    if (retried) throw new Error('Unauthorized')
    const newToken = await refreshAccessToken()
    if (!newToken) throw new Error('Unauthorized')
    return request(path, options, true)
  }

  if (!res.ok) {
    const body = await res.json().catch(() => null)
    if (body) {
      // ValidationProblem: { errors: { campo: ['msg', ...] } }
      if (body.errors && typeof body.errors === 'object') {
        const msgs = (Object.values(body.errors) as string[][]).flat().join('; ')
        throw new Error(msgs || body.title || 'Erro de validação')
      }
      if (body.error) throw new Error(body.error)
      if (body.title) throw new Error(body.title)
    }
    throw new Error(res.statusText || `Erro ${res.status}`)
  }

  if (res.status === 204) return undefined as T
  return res.json()
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  getText: (path: string) => requestText(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
