import { useState, useEffect } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { Menu } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import Sidebar from './Sidebar'
import { cn } from '@/lib/utils'

export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth()

  const [collapsed, setCollapsed] = useState<boolean>(() => {
    try { return localStorage.getItem('sidebar-collapsed') === 'true' }
    catch { return false }
  })

  const [mobileOpen, setMobileOpen] = useState(false)

  useEffect(() => {
    try { localStorage.setItem('sidebar-collapsed', String(collapsed)) }
    catch { /* ignore */ }
  }, [collapsed])

  useEffect(() => {
    const handler = () => { if (window.innerWidth >= 1024) setMobileOpen(false) }
    window.addEventListener('resize', handler)
    return () => window.removeEventListener('resize', handler)
  }, [])

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
      {/* Mobile top bar */}
      <header className="lg:hidden fixed top-0 left-0 right-0 z-30 h-14 flex items-center gap-3 px-4 bg-sidebar border-b border-sidebar-border">
        <button
          onClick={() => setMobileOpen(true)}
          className="p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
          aria-label="Abrir menu"
        >
          <Menu className="h-5 w-5" />
        </button>
      </header>

      {/* GestorAI badge — always visible, top-right */}
      <div className="fixed top-2 right-3 z-50 flex items-center gap-1.5 bg-black/10 dark:bg-white/10 backdrop-blur-sm rounded-full px-2 py-1 pointer-events-none select-none">
        <img src="/logo-gestorai.png" alt="GestorAI" className="h-4 w-4 object-contain opacity-70" />
        <span className="text-[10px] font-medium text-foreground/50 tracking-wide">GestorAI</span>
      </div>

      {/* Backdrop for mobile drawer */}
      {mobileOpen && (
        <div
          className="lg:hidden fixed inset-0 z-40 bg-black/50"
          onClick={() => setMobileOpen(false)}
        />
      )}

      <Sidebar
        collapsed={collapsed}
        onToggle={() => setCollapsed((v) => !v)}
        mobileOpen={mobileOpen}
        onMobileClose={() => setMobileOpen(false)}
      />

      <main
        className={cn(
          'min-h-screen transition-[margin-left] duration-300',
          'px-4 md:px-6',
          'pt-[72px] pb-4 md:pb-6 lg:pt-6',
          collapsed ? 'lg:ml-16' : 'lg:ml-64',
        )}
      >
        <Outlet />
      </main>
    </div>
  )
}
