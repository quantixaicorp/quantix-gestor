import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'

const ADMIN_URL = import.meta.env.VITE_ADMIN_URL ?? 'http://localhost:5001'
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID ?? 'gestorai'

interface AuthContextValue {
  isAuthenticated: boolean
  isLoading: boolean
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
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    setIsAuthenticated(!!localStorage.getItem('ga_token'))
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
    setIsAuthenticated(true)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    setIsAuthenticated(false)
    window.location.href = '/auth'
  }, [])

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, handleCallback }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider')
  return ctx
}
