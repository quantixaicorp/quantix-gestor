import { useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'

export default function Auth() {
  const { login } = useAuth()
  useEffect(() => {
    login().catch(console.error)
  }, [login])
  return (
    <div className="flex h-screen items-center justify-center">
      <p className="text-muted-foreground">Redirecionando para login...</p>
    </div>
  )
}
