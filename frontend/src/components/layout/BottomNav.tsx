import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, ShoppingCart, Calendar, Wallet,
  MoreHorizontal, X, ChevronDown,
  ClipboardList, FileText, CalendarDays, UserCog, Package,
  ArrowDownToLine, TrendingDown, TrendingUp, Tag, Users,
  BarChart3, Scissors, Receipt, DollarSign, Truck, Plug,
  Bot, Building2, CreditCard, LogOut,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/contexts/AuthContext'
import type { EmpresaInfo } from './AppLayout'

const PRIMARY = [
  { icon: LayoutDashboard, label: 'Início',     path: '/' },
  { icon: ShoppingCart,    label: 'Vendas',     path: '/vendas' },
  { icon: Calendar,        label: 'Agenda',     path: '/agenda' },
  { icon: Wallet,          label: 'Financeiro', path: '/financeiro' },
]

const MENU_GROUPS = [
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
      { icon: CalendarDays, label: 'Agenda Geral',  path: '/agenda' },
      { icon: Calendar,     label: 'Agendamentos',  path: '/agendamentos' },
      { icon: UserCog,      label: 'Profissionais', path: '/profissionais' },
      { icon: Scissors,     label: 'Serviços',      path: '/servicos' },
    ],
  },
  {
    label: 'Estoque',
    items: [
      { icon: Package,         label: 'Produtos',      path: '/estoque' },
      { icon: ArrowDownToLine, label: 'Movimentações', path: '/estoque/movimentacoes' },
    ],
  },
  {
    label: 'Financeiro',
    items: [
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
    label: 'Compras / Fiscal',
    items: [
      { icon: Truck,   label: 'Fornecedores',   path: '/fornecedores' },
      { icon: Receipt, label: 'Notas Fiscais',  path: '/fiscal' },
    ],
  },
  {
    label: 'Contratos',
    items: [
      { icon: FileText,    label: 'Contratos', path: '/contratos' },
      { icon: FileText,    label: 'Templates', path: '/contratos/templates' },
      { icon: DollarSign,  label: 'Cobranças', path: '/cobrancas' },
    ],
  },
  {
    label: 'Configurações',
    items: [
      { icon: Building2, label: 'Empresa',             path: '/configuracoes/empresa' },
      { icon: Plug,      label: 'Integrações',         path: '/configuracoes/integracoes' },
      { icon: Bot,       label: 'Automação',           path: '/configuracoes/automacao' },
      { icon: CreditCard, label: 'Planos',             path: '/planos' },
    ],
  },
]

interface Props {
  sidebarStyle: React.CSSProperties
  empresaConfig: EmpresaInfo | null
}

export default function BottomNav({ sidebarStyle, empresaConfig }: Props) {
  const location = useLocation()
  const navigate = useNavigate()
  const { logout } = useAuth()
  const { resolved, toggleTheme } = useTheme()
  const [menuOpen, setMenuOpen] = useState(false)
  const [openGroups, setOpenGroups] = useState<Record<string, boolean>>({})

  const isActive = (path: string) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path)

  const handleNav = (path: string) => {
    setMenuOpen(false)
    navigate(path)
  }

  const handleLogout = () => {
    setMenuOpen(false)
    logout()
    navigate('/auth')
  }

  const toggleGroup = (label: string) =>
    setOpenGroups(prev => ({ ...prev, [label]: !prev[label] }))

  return (
    <>
      {/* Bottom navigation bar */}
      <nav
        style={sidebarStyle}
        className="fixed bottom-0 left-0 right-0 z-40 md:hidden bg-sidebar border-t border-sidebar-border"
      >
        <div className="flex items-center justify-around h-16 px-2">
          {PRIMARY.map(({ icon: Icon, label, path }) => (
            <Link
              key={path}
              to={path}
              className={cn(
                'flex flex-col items-center gap-0.5 px-3 py-2 rounded-xl transition-colors',
                isActive(path)
                  ? 'text-sidebar-foreground bg-sidebar-accent'
                  : 'text-sidebar-muted'
              )}
            >
              <Icon className="h-5 w-5 shrink-0" />
              <span className="text-[10px] font-medium">{label}</span>
            </Link>
          ))}
          <button
            onClick={() => setMenuOpen(true)}
            className={cn(
              'flex flex-col items-center gap-0.5 px-3 py-2 rounded-xl transition-colors',
              menuOpen ? 'text-sidebar-foreground bg-sidebar-accent' : 'text-sidebar-muted'
            )}
          >
            <MoreHorizontal className="h-5 w-5" />
            <span className="text-[10px] font-medium">Menu</span>
          </button>
        </div>
      </nav>

      {/* Full-menu bottom sheet */}
      {menuOpen && (
        <div className="fixed inset-0 z-50 md:hidden flex flex-col justify-end">
          {/* Backdrop */}
          <div
            className="absolute inset-0 bg-black/50"
            onClick={() => setMenuOpen(false)}
          />

          {/* Sheet */}
          <div className="relative bg-background rounded-t-2xl max-h-[82vh] flex flex-col shadow-2xl">
            {/* Handle + header */}
            <div className="flex items-center justify-between px-4 pt-3 pb-2 border-b shrink-0">
              <div className="flex items-center gap-2">
                {empresaConfig?.nomeFantasia && (
                  <span className="font-semibold text-sm">{empresaConfig.nomeFantasia}</span>
                )}
              </div>
              <div className="flex items-center gap-1">
                <button
                  onClick={toggleTheme}
                  className="p-2 rounded-lg text-muted-foreground hover:bg-muted transition-colors text-xs"
                >
                  {resolved === 'dark' ? '☀️' : '🌙'}
                </button>
                <button
                  onClick={() => setMenuOpen(false)}
                  className="p-2 rounded-lg text-muted-foreground hover:bg-muted transition-colors"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            </div>

            {/* Scrollable menu */}
            <div className="overflow-y-auto flex-1 px-3 py-2">
              {MENU_GROUPS.map(group => {
                const isOpen = openGroups[group.label] !== false
                return (
                  <div key={group.label} className="mb-1">
                    <button
                      onClick={() => toggleGroup(group.label)}
                      className="w-full flex items-center justify-between px-2 py-1.5"
                    >
                      <span className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">
                        {group.label}
                      </span>
                      <ChevronDown className={cn(
                        'h-3 w-3 text-muted-foreground transition-transform duration-200',
                        !isOpen && '-rotate-90'
                      )} />
                    </button>
                    {isOpen && (
                      <div className="grid grid-cols-3 gap-1 pb-1">
                        {group.items.map(item => {
                          const Icon = item.icon
                          const active = isActive(item.path)
                          return (
                            <button
                              key={item.path}
                              onClick={() => handleNav(item.path)}
                              className={cn(
                                'flex flex-col items-center gap-1 p-2 rounded-xl text-center transition-colors',
                                active
                                  ? 'bg-primary/10 text-primary'
                                  : 'hover:bg-muted text-muted-foreground'
                              )}
                            >
                              <Icon className="h-5 w-5 shrink-0" />
                              <span className="text-[10px] font-medium leading-tight">{item.label}</span>
                            </button>
                          )
                        })}
                      </div>
                    )}
                  </div>
                )
              })}
            </div>

            {/* Logout */}
            <div className="shrink-0 border-t px-3 py-2">
              <button
                onClick={handleLogout}
                className="w-full flex items-center gap-2 px-3 py-3 rounded-xl text-destructive hover:bg-destructive/10 transition-colors"
              >
                <LogOut className="h-5 w-5" />
                <span className="text-sm font-medium">Sair</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
