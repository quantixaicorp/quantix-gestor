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
  CreditCard,
} from 'lucide-react'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'

interface MenuItem {
  icon: React.ElementType
  label: string
  path: string
}

interface MenuGroup {
  label: string
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
    items: [
      { icon: ShoppingCart,  label: 'Nova Venda',  path: '/vendas/nova' },
      { icon: ClipboardList, label: 'Histórico',   path: '/vendas' },
      { icon: FileText,      label: 'Orçamentos',  path: '/orcamentos' },
    ],
  },
  {
    label: 'Agenda',
    items: [
      { icon: CalendarDays, label: 'Agenda Geral',   path: '/agenda' },
      { icon: Calendar,     label: 'Agendamentos',   path: '/agendamentos' },
      { icon: UserCog,      label: 'Profissionais',  path: '/profissionais' },
      { icon: Scissors,     label: 'Serviços',       path: '/servicos' },
    ],
  },
  {
    label: 'Estoque',
    items: [
      { icon: Package,         label: 'Produtos',       path: '/estoque' },
      { icon: ArrowDownToLine, label: 'Movimentações',  path: '/estoque/movimentacoes' },
    ],
  },
  {
    label: 'Financeiro',
    items: [
      { icon: Wallet,       label: 'Lançamentos',      path: '/financeiro' },
      { icon: TrendingDown, label: 'Contas a Pagar',   path: '/financeiro/pagar' },
      { icon: TrendingUp,   label: 'Contas a Receber', path: '/financeiro/receber' },
      { icon: Tag,          label: 'Categorias',       path: '/financeiro/categorias' },
    ],
  },
  {
    label: 'Clientes / Relatórios',
    items: [
      { icon: Users,    label: 'Clientes',   path: '/clientes' },
      { icon: BarChart3, label: 'Relatórios', path: '/relatorios' },
    ],
  },
  {
    label: 'Compras',
    items: [
      { icon: Truck, label: 'Fornecedores', path: '/fornecedores' },
    ],
  },
  {
    label: 'Fiscal',
    items: [
      { icon: Receipt, label: 'Notas Fiscais', path: '/fiscal' },
    ],
  },
  {
    label: 'Contratos',
    items: [
      { icon: FileText,    label: 'Contratos', path: '/contratos' },
      { icon: FileText,    label: 'Templates', path: '/contratos/templates' },
      { icon: DollarSign, label: 'Cobranças',  path: '/cobrancas' },
    ],
  },
  {
    label: 'Configurações',
    items: [
      { icon: LinkIcon, label: 'Agendamento Online', path: '/configuracoes/agendamento-publico' },
      { icon: Plug, label: 'Integrações', path: '/configuracoes/integracoes' },
      { icon: CreditCard, label: 'Plano', path: '/configuracoes/plano' },
    ],
  },
  {
    label: 'Automação',
    items: [
      { icon: Bot, label: 'Configurações', path: '/configuracoes/automacao' },
      { icon: Bot, label: 'Log de envios',  path: '/automacao/log' },
    ],
  },
]

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
  mobileOpen: boolean
  onMobileClose: () => void
}

export default function Sidebar({ collapsed, onToggle, mobileOpen, onMobileClose }: SidebarProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const { logout } = useAuth()
  const { resolved, toggleTheme } = useTheme()

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
    // Close mobile drawer when a link is clicked
    onMobileClose()
  }

  const ToggleIcon = collapsed ? PanelLeftOpen : PanelLeftClose
  const ThemeIcon = resolved === 'dark' ? Sun : Moon

  // On mobile: full-width drawer (always expanded); on desktop: collapsed/expanded
  const isMobileDrawer = mobileOpen
  const showLabels = isMobileDrawer || !collapsed

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-50 h-screen bg-sidebar border-r border-sidebar-border flex flex-col',
        'transition-[width,transform] duration-300 overflow-hidden',
        'w-64',                                         // mobile always full width
        collapsed ? 'lg:w-16' : 'lg:w-64',             // desktop width by state
        mobileOpen ? 'translate-x-0' : '-translate-x-full',  // mobile show/hide
        'lg:translate-x-0',                             // always visible on desktop
      )}
    >
      {/* Logo header */}
      <div className="flex flex-col items-center justify-center border-b border-sidebar-border px-3 pt-2 pb-2 shrink-0 gap-1">
        {!showLabels ? (
          <div className="rounded-xl overflow-hidden mx-auto" style={{ width: '48px', height: '48px', position: 'relative' }}>
            <img
              src="/logo-gestorai.jpeg"
              alt="GestorAI"
              style={{
                width: 'auto',
                height: '200%',
                transform: 'translateY(-50%)',
                filter: 'invert(1) hue-rotate(180deg)',
              }}
            />
          </div>
        ) : (
          <>
            <div className="rounded-xl overflow-hidden w-full" style={{ height: '52px', position: 'relative' }}>
              <img
                src="/logo-gestorai.jpeg"
                alt="GestorAI"
                style={{
                  width: '100%',
                  height: 'auto',
                  transform: 'translateY(-50%)',
                  filter: 'invert(1) hue-rotate(180deg)',
                }}
              />
            </div>
            <span className="text-[10px] text-sidebar-muted tracking-wide">by QuantixAI</span>
          </>
        )}
      </div>

      {/* Controls bar — theme + collapse (hidden on mobile drawer) */}
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
        {/* Hide collapse toggle on mobile drawer */}
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
        {menuGroups.map((group, gi) => {
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

      {/* Logout */}
      <div className="shrink-0 border-t border-sidebar-border p-2">
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
