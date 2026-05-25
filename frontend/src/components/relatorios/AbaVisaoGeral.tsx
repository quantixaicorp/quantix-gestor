import { TrendingUp, ShoppingCart, DollarSign, AlertTriangle } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { KpisGeralResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtPct = (v: number) => `${v.toFixed(1)}%`

interface Props { kpis: KpisGeralResponse }

export default function AbaVisaoGeral({ kpis }: Props) {
  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-3">
      <KpiCard titulo="Faturamento" valor={fmt(kpis.faturamento)} icon={TrendingUp} cor="green" />
      <KpiCard titulo="Ticket médio" valor={fmt(kpis.ticketMedio)} icon={DollarSign} />
      <KpiCard titulo="Margem estimada" valor={fmtPct(kpis.margemEstimada)} icon={TrendingUp}
        cor={kpis.margemEstimada >= 20 ? 'green' : kpis.margemEstimada >= 10 ? 'yellow' : 'red'} />
      <KpiCard titulo="Total de vendas" valor={kpis.totalVendas.toString()} icon={ShoppingCart} />
      <KpiCard titulo="Inadimplência" valor={fmtPct(kpis.inadimplencia)} icon={AlertTriangle}
        cor={kpis.inadimplencia > 0 ? 'red' : 'default'}
        detalhe="Receitas vencidas / total a receber" />
    </div>
  )
}
