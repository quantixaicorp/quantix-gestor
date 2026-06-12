import { useState, useEffect, useMemo } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { Menu } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/services/api'
import Sidebar from './Sidebar'
import { cn } from '@/lib/utils'

export interface EmpresaInfo {
  logoUrl: string
  nomeFantasia: string
  corPrimaria: string
}

function hexToHsl(hex: string): [number, number, number] {
  const r = parseInt(hex.slice(1, 3), 16) / 255
  const g = parseInt(hex.slice(3, 5), 16) / 255
  const b = parseInt(hex.slice(5, 7), 16) / 255
  const max = Math.max(r, g, b), min = Math.min(r, g, b)
  let h = 0, s = 0
  const l = (max + min) / 2
  if (max !== min) {
    const d = max - min
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min)
    switch (max) {
      case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break
      case g: h = ((b - r) / d + 2) / 6; break
      case b: h = ((r - g) / d + 4) / 6; break
    }
  }
  return [Math.round(h * 360), Math.round(s * 100), Math.round(l * 100)]
}

function hslVars(h: number, s: number, l: number): string {
  return `${h} ${s}% ${Math.max(0, Math.min(100, l))}%`
}

function isLightHex(hex: string): boolean {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return (r * 299 + g * 587 + b * 114) / 1000 > 128
}

function buildSidebarStyle(cor: string | undefined): React.CSSProperties {
  if (!cor || !cor.startsWith('#') || cor.length < 7) return {}
  const [h, s, l] = hexToHsl(cor)
  const light = isLightHex(cor)
  const delta = light ? -1 : 1
  return {
    '--sidebar-background': hslVars(h, s, l),
    '--sidebar-foreground': light ? '220 20% 10%' : '210 40% 98%',
    '--sidebar-muted': light ? '220 10% 35%' : '215 16% 65%',
    '--sidebar-accent': hslVars(h, s, l + delta * 8),
    '--sidebar-border': hslVars(h, Math.max(s - 5, 0), l + delta * 12),
    '--sidebar-primary': light ? '220 20% 10%' : '210 40% 98%',
  } as React.CSSProperties
}

export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth()
  const [empresa, setEmpresa] = useState<EmpresaInfo | null>(null)

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

  useEffect(() => {
    api.get<{ logoUrl: string | null; nomeFantasia: string | null; corPrimaria: string | null }>(
      '/api/configuracao-empresa'
    )
      .then(c => setEmpresa({
        logoUrl: c.logoUrl ?? '',
        nomeFantasia: c.nomeFantasia ?? '',
        corPrimaria: c.corPrimaria ?? '',
      }))
      .catch(() => {})
  }, [])

  const sidebarStyle = useMemo(() => buildSidebarStyle(empresa?.corPrimaria), [empresa?.corPrimaria])

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
      {/* Mobile top bar — same dynamic color as sidebar */}
      <header
        style={sidebarStyle}
        className="lg:hidden fixed top-0 left-0 right-0 z-30 h-14 flex items-center gap-3 px-4 bg-sidebar border-b border-sidebar-border"
      >
        <button
          onClick={() => setMobileOpen(true)}
          className="p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
          aria-label="Abrir menu"
        >
          <Menu className="h-5 w-5" />
        </button>
      </header>

      {/* GestorAI badge — always visible, top-right */}
      <div className="fixed top-1 lg:top-2 right-3 z-50 flex items-center gap-2 bg-white dark:bg-zinc-900 border border-black/10 dark:border-white/10 shadow-sm rounded-xl px-2.5 py-1.5 pointer-events-none select-none">
        <img src="/logo-gestorai-icon.png" alt="GestorAI" className="h-8 w-8 object-contain" />
        <div className="flex flex-col leading-tight">
          <span className="text-xs font-bold text-gray-900 dark:text-white tracking-wide">GestorAI</span>
          <span className="text-[10px] text-gray-400 dark:text-gray-500 tracking-wide">by QuantixAI</span>
        </div>
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
        empresaConfig={empresa}
        sidebarStyle={sidebarStyle}
      />

      <main
        className={cn(
          'min-h-screen transition-[margin-left] duration-300',
          'pl-4 md:pl-6 pr-36 md:pr-40',
          'pt-[72px] pb-4 md:pb-6 lg:pt-6',
          collapsed ? 'lg:ml-16' : 'lg:ml-64',
        )}
      >
        <Outlet />
      </main>
    </div>
  )
}
