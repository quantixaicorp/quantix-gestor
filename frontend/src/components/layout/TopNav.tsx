import { useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, ShoppingCart, ClipboardList, FileText, Calendar,
  CalendarDays, UserCog, Package, ArrowDownToLine, Wallet, TrendingDown,
  TrendingUp, Tag, Users, BarChart3, Scissors, Receipt, DollarSign,
  Truck, Plug, Bot, Building2, CreditCard, LogOut, Sun, Moon, ChevronDown,
  Link as LinkIcon, MoreHorizontal,
} from 'lucide-react'
import { useTheme } from '@/hooks/useTheme'
import { useAuth } from '@/contexts/AuthContext'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import { cn } from '@/lib/utils'
import type { EmpresaInfo } from './AppLayout'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

interface NavItem { icon: React.ElementType; label: string; path: string }
interface NavGroup { label: string; moduleSlug?: string; items: NavItem[] }

const PRIMARY_GROUPS: NavGroup[] = [
  {
    label: 'Geral',
    items: [{ icon: LayoutDashboard, label: 'Dashboard', path: '/' }],
  },
  {
    label: 'Vendas',
    moduleSlug: 'vendas',
    items: [
      { icon: ShoppingCart,  label: 'Nova Venda', path: '/vendas/nova' },
      { icon: ClipboardList, label: 'Histórico',  path: '/vendas' },
      { icon: FileText,      label: 'Orçamentos', path: '/orcamentos' },
    ],
  },
  {
    label: 'Agenda',
    moduleSlug: 'agenda',
    items: [
      { icon: CalendarDays, label: 'Agenda Geral',  path: '/agenda' },
      { icon: Calendar,     label: 'Agendamentos',  path: '/agendamentos' },
      { icon: UserCog,      label: 'Profissionais', path: '/profissionais' },
      { icon: Scissors,     label: 'Serviços',      path: '/servicos' },
    ],
  },
  {
    label: 'Estoque',
    moduleSlug: 'estoque',
    items: [
      { icon: Package,         label: 'Produtos',      path: '/estoque' },
      { icon: ArrowDownToLine, label: 'Movimentações', path: '/estoque/movimentacoes' },
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
    label: 'Clientes',
    moduleSlug: 'clientes',
    items: [
      { icon: Users,     label: 'Clientes',   path: '/clientes' },
      { icon: BarChart3, label: 'Relatórios', path: '/relatorios' },
    ],
  },
]

const SECONDARY_GROUPS: NavGroup[] = [
  {
    label: 'Compras',
    moduleSlug: 'compras',
    items: [
      { icon: Truck,         label: 'Fornecedores', path: '/fornecedores' },
      { icon: ClipboardList, label: 'Pedidos',      path: '/compras/pedidos' },
      { icon: ShoppingCart,  label: 'Compras',      path: '/compras' },
    ],
  },
  {
    label: 'Fiscal',
    moduleSlug: 'fiscal',
    items: [{ icon: Receipt, label: 'Notas Fiscais', path: '/fiscal' }],
  },
  {
    label: 'Contratos',
    moduleSlug: 'contratos',
    items: [
      { icon: FileText,   label: 'Contratos', path: '/contratos' },
      { icon: FileText,   label: 'Templates', path: '/contratos/templates' },
      { icon: DollarSign, label: 'Cobranças', path: '/cobrancas' },
    ],
  },
  {
    label: 'Config',
    moduleSlug: 'configuracoes',
    items: [
      { icon: Building2, label: 'Empresa',            path: '/configuracoes/empresa' },
      { icon: LinkIcon,  label: 'Agendamento Online', path: '/configuracoes/agendamento-publico' },
      { icon: Plug,      label: 'Integrações',        path: '/configuracoes/integracoes' },
    ],
  },
  {
    label: 'Automação',
    moduleSlug: 'automacao',
    items: [
      { icon: Bot, label: 'Configurações', path: '/configuracoes/automacao' },
      { icon: Bot, label: 'Log de envios', path: '/automacao/log' },
    ],
  },
  {
    label: 'Assinaturas',
    moduleSlug: 'assinaturas',
    items: [{ icon: CreditCard, label: 'Planos', path: '/planos' }],
  },
]

interface DropdownState { key: string; top: number; left: number }

interface Props {
  empresaConfig: EmpresaInfo | null
  sidebarStyle: React.CSSProperties
}

export default function TopNav({ empresaConfig, sidebarStyle }: Props) {
  const location = useLocation()
  const navigate = useNavigate()
  const { logout, enabledModules, modulesLoaded } = useAuth()
  const { resolved, toggleTheme } = useTheme()
  const { obter } = useConfiguracaoEmpresa()
  const [dropdown, setDropdown] = useState<DropdownState | null>(null)
  const [isPrestador, setIsPrestador] = useState(false)
  const navRef = useRef<HTMLElement>(null)
  const dropRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    void obter().then(c => setIsPrestador((c?.tipoNegocio || 'Lojista') === 'Prestador'))
  }, [obter])

  useEffect(() => {
    function fechar(e: MouseEvent) {
      const inNav = navRef.current?.contains(e.target as Node)
      const inDrop = dropRef.current?.contains(e.target as Node)
      if (!inNav && !inDrop) setDropdown(null)
    }
    document.addEventListener('mousedown', fechar)
    return () => document.removeEventListener('mousedown', fechar)
  }, [])

  useEffect(() => { setDropdown(null) }, [location.pathname])

  const isVisible = (slug?: string) =>
    !modulesLoaded || enabledModules.size === 0 || !slug || enabledModules.has(slug)

  const visiblePrimary = PRIMARY_GROUPS.filter(g => isVisible(g.moduleSlug))
  const visibleSecondary = SECONDARY_GROUPS.filter(g => isVisible(g.moduleSlug))

  const vendaLabel = (path: string, original: string) => {
    if (!isPrestador) return original
    if (path === '/vendas/nova') return 'Nova OS'
    if (path === '/vendas') return 'Histórico de Serviços'
    return original
  }

  const isActive = (path: string) =>
    path === '/' ? location.pathname === '/' : location.pathname.startsWith(path)

  const groupActive = (group: NavGroup) => group.items.some(i => isActive(i.path))

  function openDropdown(key: string, e: React.MouseEvent<HTMLButtonElement>) {
    if (dropdown?.key === key) { setDropdown(null); return }
    const rect = e.currentTarget.getBoundingClientRect()
    const left = Math.min(rect.left, window.innerWidth - 216)
    setDropdown({ key, top: rect.bottom + 4, left })
  }

  const logoSrc = empresaConfig?.logoUrl
    ? (empresaConfig.logoUrl.startsWith('http') ? empresaConfig.logoUrl : `${API_BASE}${empresaConfig.logoUrl}`)
    : null
  const initial = (empresaConfig?.nomeFantasia?.[0] ?? 'E').toUpperCase()
  const secondaryActive = visibleSecondary.some(g => groupActive(g))
  const activeDropGroup = dropdown && dropdown.key !== '__mais__'
    ? [...visiblePrimary, ...visibleSecondary].find(g => g.label === dropdown.key)
    : null

  return (
    <>
      <nav
        ref={navRef}
        style={sidebarStyle}
        className="hidden md:flex fixed top-0 left-0 right-0 z-50 h-14 items-center bg-sidebar border-b border-sidebar-border px-3 gap-1"
      >
        {/* Logo + nome */}
        <Link to="/" className="flex items-center gap-2 px-1 mr-2 shrink-0">
          {logoSrc
            ? <img src={logoSrc} alt={empresaConfig?.nomeFantasia || ''} className="h-8 w-8 rounded-full object-cover shrink-0" />
            : <div className="h-8 w-8 rounded-full bg-sidebar-accent flex items-center justify-center text-sidebar-foreground font-bold text-sm shrink-0">{initial}</div>
          }
          {empresaConfig?.nomeFantasia && (
            <span className="text-sm font-semibold text-sidebar-foreground truncate max-w-[120px]">
              {empresaConfig.nomeFantasia}
            </span>
          )}
        </Link>

        <div className="h-6 w-px bg-sidebar-border shrink-0 mx-1" />

        {/* Primary nav groups — no overflow wrapper so dropdowns are never clipped */}
        <div className="flex items-center gap-0.5 flex-1 min-w-0">
          {visiblePrimary.map(group => {
            const active = groupActive(group)
            const isOpen = dropdown?.key === group.label

            if (group.items.length === 1) {
              const item = group.items[0]
              const Icon = item.icon
              return (
                <Link
                  key={group.label}
                  to={item.path}
                  className={cn(
                    'flex items-center gap-1.5 px-3 h-9 rounded-md text-sm font-medium whitespace-nowrap transition-colors',
                    isActive(item.path)
                      ? 'bg-sidebar-accent text-sidebar-foreground'
                      : 'text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent/60'
                  )}
                >
                  <Icon className="h-4 w-4 shrink-0" />
                  {vendaLabel(item.path, item.label)}
                </Link>
              )
            }

            return (
              <button
                key={group.label}
                onClick={e => openDropdown(group.label, e)}
                className={cn(
                  'flex items-center gap-1.5 px-3 h-9 rounded-md text-sm font-medium whitespace-nowrap transition-colors',
                  active || isOpen
                    ? 'bg-sidebar-accent text-sidebar-foreground'
                    : 'text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent/60'
                )}
              >
                {group.label}
                <ChevronDown className={cn('h-3 w-3 transition-transform duration-150', isOpen && 'rotate-180')} />
              </button>
            )
          })}

          {visibleSecondary.length > 0 && (
            <button
              onClick={e => openDropdown('__mais__', e)}
              className={cn(
                'flex items-center gap-1.5 px-3 h-9 rounded-md text-sm font-medium whitespace-nowrap transition-colors',
                secondaryActive || dropdown?.key === '__mais__'
                  ? 'bg-sidebar-accent text-sidebar-foreground'
                  : 'text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent/60'
              )}
            >
              <MoreHorizontal className="h-4 w-4" />
              Mais
              <ChevronDown className={cn('h-3 w-3 transition-transform duration-150', dropdown?.key === '__mais__' && 'rotate-180')} />
            </button>
          )}
        </div>

        {/* Right actions */}
        <div className="flex items-center gap-1 shrink-0">
          <button
            onClick={toggleTheme}
            className="p-1.5 rounded-md text-sidebar-muted hover:text-sidebar-foreground hover:bg-sidebar-accent transition-colors"
            title={resolved === 'dark' ? 'Modo claro' : 'Modo escuro'}
          >
            {resolved === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
          </button>
          <button
            onClick={() => { logout(); navigate('/auth') }}
            className="p-1.5 rounded-md text-destructive hover:bg-destructive/10 transition-colors"
            title="Sair"
          >
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </nav>

      {/* Dropdown rendered with fixed positioning — never clipped by any overflow parent */}
      {dropdown && (
        <div
          ref={dropRef}
          style={{ position: 'fixed', top: dropdown.top, left: dropdown.left, zIndex: 9999 }}
          className="min-w-[200px] rounded-xl border border-border bg-popover shadow-xl py-1"
        >
          {dropdown.key === '__mais__' ? (
            visibleSecondary.map((group, gi) => (
              <div key={group.label}>
                {gi > 0 && <div className="my-1 border-t border-border" />}
                <div className="px-3 pt-2 pb-0.5 text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">
                  {group.label}
                </div>
                {group.items.map(item => {
                  const Icon = item.icon
                  return (
                    <button
                      key={item.path}
                      onClick={() => { navigate(item.path); setDropdown(null) }}
                      className={cn(
                        'w-full flex items-center gap-2.5 px-3 py-2 text-sm transition-colors text-left',
                        isActive(item.path)
                          ? 'bg-primary/10 text-primary font-medium'
                          : 'text-foreground hover:bg-accent'
                      )}
                    >
                      <Icon className="h-4 w-4 shrink-0 text-muted-foreground" />
                      {vendaLabel(item.path, item.label)}
                    </button>
                  )
                })}
              </div>
            ))
          ) : (
            activeDropGroup?.items.map(item => {
              const Icon = item.icon
              return (
                <button
                  key={item.path}
                  onClick={() => { navigate(item.path); setDropdown(null) }}
                  className={cn(
                    'w-full flex items-center gap-2.5 px-3 py-2 text-sm transition-colors text-left',
                    isActive(item.path)
                      ? 'bg-primary/10 text-primary font-medium'
                      : 'text-foreground hover:bg-accent'
                  )}
                >
                  <Icon className="h-4 w-4 shrink-0 text-muted-foreground" />
                  {vendaLabel(item.path, item.label)}
                </button>
              )
            })
          )}
        </div>
      )}
    </>
  )
}
