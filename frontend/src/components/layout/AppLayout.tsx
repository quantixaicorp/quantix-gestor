import { useState, useEffect, useMemo } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { Menu } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { api } from '@/services/api'
import Sidebar from './Sidebar'
import BottomNav from './BottomNav'
import TopNav from './TopNav'
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

  const [layoutMode, setLayoutMode] = useState<'sidebar' | 'topnav'>(() => {
    try { return (localStorage.getItem('layout-mode') as 'sidebar' | 'topnav') || 'sidebar' }
    catch { return 'sidebar' }
  })

  // Expose layout mode change globally so settings page can trigger it
  useEffect(() => {
    const handler = (e: CustomEvent) => {
      const mode = e.detail as 'sidebar' | 'topnav'
      setLayoutMode(mode)
    }
    window.addEventListener('layout-mode-change', handler as EventListener)
    return () => window.removeEventListener('layout-mode-change', handler as EventListener)
  }, [])

  const [collapsed, setCollapsed] = useState<boolean>(() => {
    try { return localStorage.getItem('sidebar-collapsed') === 'true' }
    catch { return false }
  })

  // Tablet (md–lg): sidebar always collapsed/icon-only
  const [isTablet, setIsTablet] = useState(() =>
    typeof window !== 'undefined' && window.innerWidth >= 768 && window.innerWidth < 1024
  )

  const [, setMobileOpen] = useState(false)

  useEffect(() => {
    const check = () => {
      const w = window.innerWidth
      setIsTablet(w >= 768 && w < 1024)
      if (w >= 768) setMobileOpen(false)
    }
    window.addEventListener('resize', check)
    return () => window.removeEventListener('resize', check)
  }, [])

  useEffect(() => {
    try { localStorage.setItem('sidebar-collapsed', String(collapsed)) }
    catch { /* ignore */ }
  }, [collapsed])

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

  // Tablet forces sidebar collapsed; desktop respects user pref
  const effectiveCollapsed = isTablet || collapsed

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-muted-foreground">Carregando...</p>
      </div>
    )
  }

  if (!isAuthenticated) return <Navigate to="/auth" replace />

  const isTopNav = layoutMode === 'topnav'

  return (
    <div className="min-h-screen bg-background">
      {isTopNav ? (
        /* ── TopNav mode ─────────────────────────────────────────── */
        <TopNav empresaConfig={empresa} sidebarStyle={sidebarStyle} />
      ) : (
        /* ── Sidebar mode ────────────────────────────────────────── */
        <>
          {/* Tablet top bar — hamburger only, sidebar mode */}
          <header
            style={{ ...sidebarStyle, marginLeft: effectiveCollapsed ? '4rem' : '16rem' } as React.CSSProperties}
            className="hidden md:flex lg:hidden fixed top-0 left-0 right-0 z-30 h-14 items-center gap-3 px-4 bg-sidebar border-b border-sidebar-border"
          >
            <button
              onClick={() => setCollapsed(v => !v)}
              className="p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
              aria-label="Expandir menu"
            >
              <Menu className="h-5 w-5" />
            </button>
          </header>

          <Sidebar
            collapsed={effectiveCollapsed}
            onToggle={() => !isTablet && setCollapsed(v => !v)}
            mobileOpen={false}
            onMobileClose={() => {}}
            empresaConfig={empresa}
            sidebarStyle={sidebarStyle}
          />
        </>
      )}

      {/* Bottom navigation — mobile only, always shown */}
      <BottomNav sidebarStyle={sidebarStyle} empresaConfig={empresa} />

      <main
        className={cn(
          'min-h-screen transition-[margin-left] duration-300',
          'px-4 md:px-6',
          'pt-4 pb-20',
          isTopNav
            ? 'md:pt-16 md:pb-6 md:ml-0'
            : cn(
                'md:pt-[72px] md:pb-6',
                'lg:pt-6 lg:pb-6',
                effectiveCollapsed ? 'md:ml-16' : 'md:ml-64',
              )
        )}
      >
        <Outlet />
      </main>
    </div>
  )
}
