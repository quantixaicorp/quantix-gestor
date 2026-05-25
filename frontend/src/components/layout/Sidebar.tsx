import { NavLink } from 'react-router-dom'
import { LayoutDashboard, ShoppingCart, Package, Wallet, Users, BarChart3, ArrowDownToLine, ClipboardList, TrendingDown, TrendingUp, FileText, Calendar, UserCog } from 'lucide-react'
import { cn } from '@/lib/utils'

const links = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/vendas/nova', icon: ShoppingCart, label: 'Nova Venda' },
  { to: '/vendas', icon: ClipboardList, label: 'Histórico' },
  { to: '/orcamentos', icon: FileText, label: 'Orçamentos' },
  { to: '/agendamentos', icon: Calendar, label: 'Agendamentos' },
  { to: '/profissionais', icon: UserCog, label: 'Profissionais' },
  { to: '/estoque', icon: Package, label: 'Produtos' },
  { to: '/estoque/movimentacoes', icon: ArrowDownToLine, label: 'Movimentações' },
  { to: '/financeiro', icon: Wallet, label: 'Lançamentos' },
  { to: '/financeiro/pagar', icon: TrendingDown, label: 'Contas a Pagar' },
  { to: '/financeiro/receber', icon: TrendingUp, label: 'Contas a Receber' },
  { to: '/clientes', icon: Users, label: 'Clientes' },
  { to: '/relatorios', icon: BarChart3, label: 'Relatórios' },
]

export default function Sidebar() {
  return (
    <aside className="flex h-screen w-56 flex-col border-r bg-card px-3 py-4">
      <div className="mb-6 px-2">
        <span className="text-xl font-bold text-primary">GestorAI</span>
      </div>
      <nav className="flex flex-col gap-1">
        {links.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
