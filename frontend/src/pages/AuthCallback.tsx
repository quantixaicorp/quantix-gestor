import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

export default function AuthCallback() {
  const { handleCallback } = useAuth()
  const navigate = useNavigate()
  const ran = useRef(false)

  useEffect(() => {
    if (ran.current) return
    ran.current = true

    const code = new URLSearchParams(window.location.search).get('code')
    if (!code) { navigate('/auth'); return }

    handleCallback(code)
      .then(() => navigate('/'))
      .catch(() => navigate('/auth'))
  }, [handleCallback, navigate])

  return (
    <div className="flex h-screen items-center justify-center">
      <p className="text-muted-foreground">Autenticando...</p>
    </div>
  )
}
