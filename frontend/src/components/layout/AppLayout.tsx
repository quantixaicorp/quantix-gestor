import { useState, useEffect } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import Sidebar from './Sidebar'

export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth()

  const [collapsed, setCollapsed] = useState<boolean>(() => {
    try { return localStorage.getItem('sidebar-collapsed') === 'true' }
    catch { return false }
  })

  useEffect(() => {
    try { localStorage.setItem('sidebar-collapsed', String(collapsed)) }
    catch { /* ignore */ }
  }, [collapsed])

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-muted-foreground">Carregando...</p>
      </div>
    )
  }

  if (!isAuthenticated) return <Navigate to="/auth" replace />

  return (
    <div className="min-h-screen bg-background">
      <Sidebar collapsed={collapsed} onToggle={() => setCollapsed((v) => !v)} />
      <main
        className="min-h-screen transition-[margin-left] duration-300 p-6"
        style={{ marginLeft: collapsed ? '4rem' : '16rem' }}
      >
        <Outlet />
      </main>
    </div>
  )
}
