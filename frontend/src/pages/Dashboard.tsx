import { useEffect } from 'react'
import {
  TrendingUp, ShoppingCart, PackageX,
  AlertTriangle, ArrowUpCircle, Wallet,
} from 'lucide-react'
import { useDashboard } from '@/hooks/useDashboard'
import KpiCard from '@/components/dashboard/KpiCard'
import GraficoVendas from '@/components/dashboard/GraficoVendas'
import GraficoFluxo from '@/components/dashboard/GraficoFluxo'
import TopProdutos from '@/components/dashboard/TopProdutos'

const fmt = (v: number | null | undefined) => (v ?? 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function Dashboard() {
  const { data, loading, error, load } = useDashboard()

  useEffect(() => { void load() }, [load])

  if (loading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Carregando dashboard...</p>
      </div>
    )
  }

  if (error || !data) {
    return (
      <div className="flex h-64 items-center justify-center flex-col gap-2">
        <p className="text-destructive font-medium">Erro ao carregar dashboard</p>
        <p className="text-sm text-muted-foreground">{error ?? 'Sem dados'}</p>
        <button onClick={() => void load()} className="text-sm text-primary underline">Tentar novamente</button>
      </div>
    )
  }

  const { kpis } = data
  const saldoMes = kpis.totalReceitasMes - kpis.totalDespesasMes

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <p className="text-sm text-muted-foreground">
          {new Date().toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })}
        </p>
      </div>

      <div className="rounded-xl border bg-card p-4 space-y-3">
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Vendas</p>
        <div className="grid grid-cols-2 gap-4">
          <KpiCard titulo="Vendido hoje" valor={fmt(kpis.totalVendidoHoje)} icon={ShoppingCart} cor="green" />
          <KpiCard titulo="Vendido no mês" valor={fmt(kpis.totalVendidoMes)} icon={TrendingUp} />
        </div>
      </div>

      <div className="rounded-xl border bg-card p-4 space-y-3">
        <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide">Financeiro — mês atual</p>
        <div className="grid grid-cols-3 gap-4">
          <KpiCard titulo="Saldo do mês" valor={fmt(saldoMes)} icon={Wallet}
            cor={saldoMes >= 0 ? 'green' : 'red'} />
          <KpiCard titulo="A receber (pendente)" valor={fmt(kpis.contasReceberPendentes)} icon={ArrowUpCircle} cor="green" />
          <KpiCard titulo="Contas vencidas" valor={fmt(kpis.contasPagarVencidas)} icon={AlertTriangle}
            cor={kpis.contasPagarVencidas > 0 ? 'red' : 'default'} />
        </div>
      </div>

      {kpis.produtosEstoqueBaixo > 0 && (
        <div className="flex items-center gap-2 rounded-md border border-yellow-500/50 bg-yellow-50 p-3 dark:bg-yellow-950/20">
          <PackageX size={18} className="text-yellow-600 shrink-0" />
          <p className="text-sm text-yellow-700 dark:text-yellow-400">
            <strong>{kpis.produtosEstoqueBaixo} produto(s)</strong> com estoque abaixo do mínimo.{' '}
            <a href="/estoque" className="underline font-medium">Ver estoque →</a>
          </p>
        </div>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <GraficoVendas dados={data.vendasUltimos7Dias ?? []} />
        <GraficoFluxo dados={data.fluxoMes ?? []} />
      </div>

      <TopProdutos dados={data.topProdutos ?? []} />
    </div>
  )
}
