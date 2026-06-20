import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'

const ADMIN_URL = import.meta.env.VITE_ADMIN_URL ?? 'http://localhost:5001'
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID ?? 'gestorai'

interface JwtPayload {
  roles?: string | string[]
  is_superadmin?: string
  name?: string
  sub?: string
  modules?: string | string[]
}

function parseJwt(token: string): JwtPayload | null {
  try {
    const payload = token.split('.')[1]
    const bytes = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    const decoded = decodeURIComponent(
      bytes.split('').map(c => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join('')
    )
    return JSON.parse(decoded) as JwtPayload
  } catch { return null }
}

interface AuthContextValue {
  isAuthenticated: boolean
  isAdmin: boolean
  isLoading: boolean
  userName: string | null
  enabledModules: Set<string>
  modulesLoaded: boolean
  login: () => Promise<void>
  logout: () => void
  handleCallback: (code: string) => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function generateCodeVerifier(): string {
  const array = new Uint8Array(32)
  crypto.getRandomValues(array)
  return btoa(String.fromCharCode(...array))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier)
  const digest = await crypto.subtle.digest('SHA-256', data)
  return btoa(String.fromCharCode(...new Uint8Array(digest)))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isAdmin, setIsAdmin] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [userName, setUserName] = useState<string | null>(null)
  const [enabledModules, setEnabledModules] = useState<Set<string>>(new Set())
  const [modulesLoaded, setModulesLoaded] = useState(false)

  function applyToken(token: string | null) {
    if (!token) {
      setIsAuthenticated(false)
      setIsAdmin(false)
      setUserName(null)
      setEnabledModules(new Set())
      setModulesLoaded(true)
      return
    }
    const payload = parseJwt(token)
    const roles = payload?.roles
    const hasAdmin = payload?.is_superadmin === 'true' ||
      (Array.isArray(roles) ? roles.includes('admin') : roles === 'admin')

    // Read modules from JWT immediately — no async call needed for initial render.
    // fetchModules() will override with fresh DB data afterwards.
    const modulesClaim = payload?.modules
    const moduleSlugs = Array.isArray(modulesClaim)
      ? modulesClaim
      : modulesClaim ? [modulesClaim] : []

    setIsAuthenticated(true)
    setIsAdmin(hasAdmin)
    setUserName(payload?.name ?? null)
    setEnabledModules(new Set(moduleSlugs))
    setModulesLoaded(true)
  }

  async function fetchModules(token: string) {
    try {
      const res = await fetch(`${ADMIN_URL}/api/me/modules`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      if (res.ok) {
        // Backend returns PascalCase by default (no global camelCase policy configured)
        const data = await res.json() as { Modules?: string[]; modules?: string[] }
        const slugs = data.Modules ?? data.modules ?? []
        setEnabledModules(new Set(slugs))
      }
    } catch { /* non-critical — sidebar shows everything on error */ }
    finally { setModulesLoaded(true) }
  }

  useEffect(() => {
    const token = localStorage.getItem('ga_token')
    applyToken(token)          // sets enabledModules from JWT instantly
    if (token) void fetchModules(token)  // overrides with fresh DB data
    setIsLoading(false)
  }, [])

  const login = useCallback(async () => {
    const verifier = generateCodeVerifier()
    const challenge = await generateCodeChallenge(verifier)
    localStorage.setItem('pkce_verifier', verifier)

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: CLIENT_ID,
      redirect_uri: `${window.location.origin}/auth/callback`,
      scope: 'openid profile offline_access quantix',
      code_challenge: challenge,
      code_challenge_method: 'S256',
    })
    window.location.href = `${ADMIN_URL}/connect/authorize?${params}`
  }, [])

  const handleCallback = useCallback(async (code: string) => {
    const verifier = localStorage.getItem('pkce_verifier')
    if (!verifier) throw new Error('PKCE verifier ausente')

    const res = await fetch(`${ADMIN_URL}/connect/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        client_id: CLIENT_ID,
        code,
        redirect_uri: `${window.location.origin}/auth/callback`,
        code_verifier: verifier,
      }),
    })

    if (!res.ok) throw new Error('Falha na troca do código')

    const tokens = await res.json() as { access_token?: string; refresh_token?: string }
    if (!tokens.access_token || !tokens.refresh_token) throw new Error('Tokens ausentes na resposta')
    localStorage.setItem('ga_token', tokens.access_token)
    localStorage.setItem('ga_refresh_token', tokens.refresh_token)
    localStorage.removeItem('pkce_verifier')
    applyToken(tokens.access_token)
    await fetchModules(tokens.access_token)
  }, [])

  const logout = useCallback(() => {
    const token = localStorage.getItem('ga_token')
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    localStorage.removeItem('pkce_verifier')
    setIsAuthenticated(false)
    setIsAdmin(false)
    setUserName(null)
    setEnabledModules(new Set())
    setModulesLoaded(false)
    const params = new URLSearchParams({
      post_logout_redirect_uri: window.location.origin,
      ...(token ? { id_token_hint: token } : {}),
    })
    window.location.href = `${ADMIN_URL}/connect/endsession?${params}`
  }, [])

  return (
    <AuthContext.Provider value={{
      isAuthenticated, isAdmin, isLoading, userName,
      enabledModules, modulesLoaded,
      login, logout, handleCallback,
    }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider')
  return ctx
}
