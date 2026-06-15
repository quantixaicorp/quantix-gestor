import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard,
  ShoppingCart,
  ClipboardList,
  FileText,
  Calendar,
  CalendarDays,
  UserCog,
  Package,
  ArrowDownToLine,
  Wallet,
  TrendingDown,
  TrendingUp,
  Tag,
  Users,
  BarChart3,
  Scissors,
  Receipt,
  DollarSign,
  LogOut,
  PanelLeftClose,
  PanelLeftOpen,
  Sun,
  Moon,
  ChevronDown,
  Link as LinkIcon,
  Truck,
  Plug,
  Bot,
  Building2,
  CreditCard,
} from 'lucide-react'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'
import type { EmpresaInfo } from './AppLayout'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface MenuItem {
  icon: React.ElementType
  label: string
  path: string
}

interface MenuGroup {
  label: string
  moduleSlug?: string  // undefined = always visible (e.g. Dashboard)
  items: MenuItem[]
}

const menuGroups: MenuGroup[] = [
  {
    label: 'Geral',
    items: [
      { icon: LayoutDashboard, label: 'Dashboard', path: '/' },
    ],
  },
  {
    label: 'Vendas',
    moduleSlug: 'vendas',
    items: [
      { icon: ShoppingCart,  label: 'Nova Venda',  path: '/vendas/nova' },
      { icon: ClipboardList, label: 'Histórico',   path: '/vendas' },
      { icon: FileText,      label: 'Orçamentos',  path: '/orcamentos' },
    ],
  },
  {
    label: 'Agenda',
    moduleSlug: 'agenda',
    items: [
      { icon: CalendarDays, label: 'Agenda Geral',   path: '/agenda' },
      { icon: Calendar,     label: 'Agendamentos',   path: '/agendamentos' },
      { icon: UserCog,      label: 'Profissionais',  path: '/profissionais' },
      { icon: Scissors,     label: 'Serviços',       path: '/servicos' },
    ],
  },
  {
    label: 'Estoque',
    moduleSlug: 'estoque',
    items: [
      { icon: Package,         label: 'Produtos',       path: '/estoque' },
      { icon: ArrowDownToLine, label: 'Movimentações',  path: '/estoque/movimentacoes' },
    ],
  },
  {
    label: 'Financeiro',
    moduleSlug: 'financeiro',
    items: [
      { icon: Wallet,       label: 'Lançamentos',      path: '/financeiro' },
      { icon: TrendingDown, label: 'Contas a Pagar',   path: '/financeiro/pagar' },
      { icon: TrendingUp,   label: 'Contas a Receber', path: '/financeiro/receber' },
      { icon: Tag,          label: 'Categorias',       path: '/financeiro/categorias' },
    ],
  },
  {
    label: 'Clientes / Relatórios',
    moduleSlug: 'clientes',
    items: [
      { icon: Users,    label: 'Clientes',   path: '/clientes' },
      { icon: BarChart3, label: 'Relatórios', path: '/relatorios' },
    ],
  },
  {
    label: 'Compras',
    moduleSlug: 'compras',
    items: [
      { icon: Truck,         label: 'Fornecedores', path: '/fornecedores' },
      { icon: ClipboardList, label: 'Pedidos',       path: '/compras/pedidos' },
      { icon: ShoppingCart,  label: 'Compras',       path: '/compras' },
      { icon: BarChart3,     label: 'Dashboard',     path: '/compras/dashboard' },
    ],
  },
  {
    label: 'Fiscal',
    moduleSlug: 'fiscal',
    items: [
      { icon: Receipt, label: 'Notas Fiscais', path: '/fiscal' },
    ],
  },
  {
    label: 'Contratos',
    moduleSlug: 'contratos',
    items: [
      { icon: FileText,    label: 'Contratos', path: '/contratos' },
      { icon: FileText,    label: 'Templates', path: '/contratos/templates' },
      { icon: DollarSign, label: 'Cobranças',  path: '/cobrancas' },
    ],
  },
  {
    label: 'Configurações',
    moduleSlug: 'configuracoes',
    items: [
      { icon: Building2, label: 'Empresa',              path: '/configuracoes/empresa' },
      { icon: LinkIcon,  label: 'Agendamento Online',   path: '/configuracoes/agendamento-publico' },
      { icon: Plug,      label: 'Integrações',          path: '/configuracoes/integracoes' },
    ],
  },
  {
    label: 'Automação',
    moduleSlug: 'automacao',
    items: [
      { icon: Bot, label: 'Configurações', path: '/configuracoes/automacao' },
      { icon: Bot, label: 'Log de envios',  path: '/automacao/log' },
    ],
  },
  {
    label: 'Assinaturas',
    moduleSlug: 'assinaturas',
    items: [
      { icon: CreditCard, label: 'Planos', path: '/planos' },
    ],
  },
]

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
  mobileOpen: boolean
  onMobileClose: () => void
  empresaConfig: EmpresaInfo | null
  sidebarStyle: React.CSSProperties
}

export default function Sidebar({ collapsed, onToggle, mobileOpen, onMobileClose, empresaConfig, sidebarStyle }: SidebarProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const { logout, enabledModules, modulesLoaded } = useAuth()
  const { resolved, toggleTheme } = useTheme()
  const empresa = empresaConfig

  // Show all groups when: still loading, OR no modules configured (empty = no restrictions).
  // Only filter when the company has at least one module configured.
  const visibleGroups = (!modulesLoaded || enabledModules.size === 0)
    ? menuGroups
    : menuGroups.filter(g => !g.moduleSlug || enabledModules.has(g.moduleSlug))

  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>(() => {
    try {
      const saved = localStorage.getItem('sidebar-groups')
      if (saved) return JSON.parse(saved) as Record<string, boolean>
    } catch { /* ignore */ }
    return Object.fromEntries(menuGroups.map((g) => [g.label, true]))
  })

  const toggleGroup = (label: string) => {
    setOpenGroups((prev) => {
      const next = { ...prev, [label]: !prev[label] }
      try { localStorage.setItem('sidebar-groups', JSON.stringify(next)) } catch { /* ignore */ }
      return next
    })
  }

  const handleLogout = () => {
    logout()
    navigate('/auth')
  }

  const handleNavClick = () => {
    onMobileClose()
  }

  const ToggleIcon = collapsed ? PanelLeftOpen : PanelLeftClose
  const ThemeIcon = resolved === 'dark' ? Sun : Moon

  const isMobileDrawer = mobileOpen
  const showLabels = isMobileDrawer || !collapsed

  const logoSrc = empresa?.logoUrl
    ? (empresa.logoUrl.startsWith('http') ? empresa.logoUrl : `${API_BASE}${empresa.logoUrl}`)
    : null

  const initial = (empresa?.nomeFantasia?.[0] ?? 'E').toUpperCase()

  return (
    <aside
      style={sidebarStyle}
      className={cn(
        'fixed left-0 top-0 z-50 h-screen bg-sidebar border-r border-sidebar-border flex flex-col',
        'transition-[width,transform] duration-300 overflow-hidden',
        'hidden md:flex',
        'md:w-16',
        collapsed ? 'lg:w-16' : 'lg:w-64',
      )}
    >
      {/* Company logo header */}
      <div className="flex flex-col items-center justify-center border-b border-sidebar-border px-3 pt-3 pb-2 shrink-0 gap-1">
        {!showLabels ? (
          logoSrc
            ? <img src={logoSrc} alt={empresa?.nomeFantasia || 'Empresa'}
                className="rounded-full mx-auto object-cover"
                style={{ width: '48px', height: '48px' }} />
            : <div className="w-12 h-12 rounded-full bg-sidebar-accent flex items-center justify-center text-sidebar-foreground font-bold text-lg">
                {initial}
              </div>
        ) : (
          <div className="flex items-center gap-3 w-full">
            {logoSrc
              ? <img src={logoSrc} alt={empresa?.nomeFantasia || 'Empresa'}
                  className="rounded-full object-cover shrink-0"
                  style={{ width: '48px', height: '48px' }} />
              : <div className="w-12 h-12 rounded-full bg-sidebar-accent flex items-center justify-center text-sidebar-foreground font-bold text-lg shrink-0">
                  {initial}
                </div>
            }
            {empresa?.nomeFantasia && (
              <span className="text-sm font-semibold text-sidebar-foreground truncate leading-tight">
                {empresa.nomeFantasia}
              </span>
            )}
          </div>
        )}
      </div>

      {/* Controls bar — theme + collapse */}
      <div className={cn(
        'border-b border-sidebar-border shrink-0',
        !showLabels ? 'flex flex-col items-center gap-1 py-2' : 'flex justify-end gap-1 px-3 py-1'
      )}>
        <button
          onClick={toggleTheme}
          className="p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
          title={resolved === 'dark' ? 'Modo claro' : 'Modo escuro'}
        >
          <ThemeIcon className="h-4 w-4" />
        </button>
        <button
          onClick={onToggle}
          className="hidden lg:block p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
          title={collapsed ? 'Expandir menu' : 'Retrair menu'}
        >
          <ToggleIcon className="h-4 w-4" />
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-2 px-2">
        {visibleGroups.map((group, gi) => {
          const isOpen = !showLabels || openGroups[group.label] !== false
          return (
            <div key={group.label} className={cn(gi > 0 && 'mt-1')}>
              {showLabels && (
                <button
                  onClick={() => toggleGroup(group.label)}
                  className="w-full flex items-center justify-between px-2 pt-2 pb-1"
                >
                  <span className="text-[10px] font-semibold uppercase tracking-widest text-sidebar-muted">
                    {group.label}
                  </span>
                  <ChevronDown className={cn(
                    'h-3 w-3 text-sidebar-muted transition-transform duration-200',
                    !isOpen && '-rotate-90'
                  )} />
                </button>
              )}
              {!showLabels && gi > 0 && (
                <div className="my-2 mx-2 border-t border-sidebar-border" />
              )}

              {isOpen && group.items.map((item) => {
                const isActive =
                  location.pathname === item.path ||
                  (item.path !== '/' && location.pathname.startsWith(item.path))
                const Icon = item.icon

                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    title={!showLabels ? item.label : undefined}
                    onClick={handleNavClick}
                    className={cn(
                      'sidebar-link',
                      isActive && 'sidebar-link-active',
                      !showLabels && 'justify-center px-0'
                    )}
                  >
                    <Icon className="h-5 w-5 shrink-0" />
                    {showLabels && <span>{item.label}</span>}
                  </Link>
                )
              })}
            </div>
          )
        })}
      </nav>

      {/* GestorAI brand + Logout */}
      <div className="shrink-0 border-t border-sidebar-border p-2 space-y-1">
        <div className={cn(
          'flex items-center gap-2 px-2 py-1.5 rounded-lg',
          !showLabels && 'justify-center px-0'
        )}>
          <img src="/logo-gestorai-icon.png" alt="GestorAI" className="h-6 w-6 object-contain shrink-0 opacity-80" />
          {showLabels && (
            <div className="flex flex-col leading-tight min-w-0">
              <span className="text-[11px] font-bold text-sidebar-foreground/70 tracking-wide">GestorAI</span>
              <span className="text-[9px] text-sidebar-muted tracking-wide">by QuantixAI</span>
            </div>
          )}
        </div>

        <button
          onClick={handleLogout}
          title={!showLabels ? 'Sair' : undefined}
          className={cn(
            'sidebar-link w-full text-destructive hover:bg-destructive/10 hover:text-destructive',
            !showLabels && 'justify-center px-0'
          )}
        >
          <LogOut className="h-5 w-5 shrink-0" />
          {showLabels && <span>Sair</span>}
        </button>
      </div>
    </aside>
  )
}
