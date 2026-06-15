import {
  TrendingUp, ShoppingCart, DollarSign,
  AlertTriangle, Users, Wallet, ArrowUpCircle, ArrowDownCircle,
} from 'lucide-react'
import {
  AreaChart, Area, ComposedChart, Bar, Line,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import KpiCard from '@/components/dashboard/KpiCard'
import type { KpisGeralResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtN = (v: number) => v.toLocaleString('pt-BR')
const fmtPct = (v: number) => `${v.toFixed(1)}%`
const fmtDia = (d: string) => new Date(d).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })

interface Props { kpis: KpisGeralResponse }

function Panel({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border bg-muted/20 p-3 sm:p-5 space-y-4">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{titulo}</p>
      {children}
    </div>
  )
}

export default function AbaVisaoGeral({ kpis }: Props) {
  const tendencia = kpis.tendenciaVendas.map(d => ({
    dia: fmtDia(d.data as unknown as string),
    total: d.total,
    quantidade: d.quantidade,
  }))

  const fluxo = kpis.fluxoPorDia.map(d => ({
    dia: fmtDia(d.data as unknown as string),
    receitas: d.receitas,
    despesas: d.despesas,
    saldo: d.saldo,
  }))

  return (
    <div className="space-y-5">

      {/* ── Vendas ─────────────────────────────────────────── */}
      <Panel titulo="Vendas do Período">
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
          <KpiCard titulo="Faturamento" valor={fmt(kpis.faturamento)} icon={TrendingUp} cor="green" />
          <KpiCard titulo="Ticket médio" valor={fmt(kpis.ticketMedio)} icon={DollarSign} />
          <KpiCard titulo="Margem estimada" valor={fmtPct(kpis.margemEstimada)} icon={TrendingUp}
            cor={kpis.margemEstimada >= 20 ? 'green' : kpis.margemEstimada >= 10 ? 'yellow' : 'red'} />
          <KpiCard titulo="Total de vendas" valor={fmtN(kpis.totalVendas)} icon={ShoppingCart} />
          <KpiCard titulo="Clientes atendidos" valor={fmtN(kpis.clientesAtendidos)} icon={Users} />
        </div>

        {tendencia.length > 0 ? (
          <div className="rounded-lg bg-card border p-4">
            <p className="text-sm font-medium mb-3">Tendência de vendas</p>
            <ResponsiveContainer width="100%" height={200}>
              <AreaChart data={tendencia} margin={{ left: 0, right: 4 }}>
                <defs>
                  <linearGradient id="gradVendas" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="hsl(221 83% 53%)" stopOpacity={0.2} />
                    <stop offset="95%" stopColor="hsl(221 83% 53%)" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                <XAxis dataKey="dia" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false}
                  tickFormatter={v => `R$${((v as number) / 1000).toFixed(0)}k`} />
                <Tooltip
                  formatter={(v: unknown) => [fmt(v as number), 'Total']}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }}
                />
                <Area type="monotone" dataKey="total" stroke="hsl(221 83% 53%)"
                  strokeWidth={2} fill="url(#gradVendas)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="rounded-lg bg-card border p-4 flex h-28 items-center justify-center text-sm text-muted-foreground">
            Sem vendas no período
          </div>
        )}

        {kpis.topProdutos.length > 0 && (
          <div className="grid gap-4 lg:grid-cols-2">
            <div className="rounded-lg bg-card border overflow-hidden">
              <div className="px-4 py-3 border-b bg-muted/30">
                <p className="text-sm font-semibold">Top Produtos</p>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-muted/40">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">#</th>
                    <th className="text-left px-4 py-2 font-medium text-muted-foreground">Produto</th>
                    <th className="text-right px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Qtd</th>
                    <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {kpis.topProdutos.map((p, i) => (
                    <tr key={i} className="border-t hover:bg-muted/20">
                      <td className="px-4 py-2 text-muted-foreground hidden sm:table-cell">{i + 1}</td>
                      <td className="px-4 py-2 font-medium">{p.nome}</td>
                      <td className="px-4 py-2 text-right text-muted-foreground hidden sm:table-cell">{fmtN(p.quantidade)}</td>
                      <td className="px-4 py-2 text-right font-medium">{fmt(p.total)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {kpis.topClientes.length > 0 && (
              <div className="rounded-lg bg-card border overflow-hidden">
                <div className="px-4 py-3 border-b bg-muted/30">
                  <p className="text-sm font-semibold">Top Clientes</p>
                </div>
                <table className="w-full text-sm">
                  <thead className="bg-muted/40">
                    <tr>
                      <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">#</th>
                      <th className="text-left px-4 py-2 font-medium text-muted-foreground">Cliente</th>
                      <th className="text-right px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Compras</th>
                      <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {kpis.topClientes.map((c, i) => (
                      <tr key={i} className="border-t hover:bg-muted/20">
                        <td className="px-4 py-2 text-muted-foreground hidden sm:table-cell">{i + 1}</td>
                        <td className="px-4 py-2 font-medium">{c.nome}</td>
                        <td className="px-4 py-2 text-right text-muted-foreground hidden sm:table-cell">{fmtN(c.compras)}</td>
                        <td className="px-4 py-2 text-right font-medium">{fmt(c.total)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </Panel>

      {/* ── Financeiro ─────────────────────────────────────── */}
      <Panel titulo="Financeiro do Período">
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <KpiCard titulo="Receitas pagas" valor={fmt(kpis.totalReceitas)} icon={ArrowUpCircle} cor="green" />
          <KpiCard titulo="Despesas pagas" valor={fmt(kpis.totalDespesas)} icon={ArrowDownCircle}
            cor={kpis.totalDespesas > 0 ? 'red' : 'default'} />
          <KpiCard titulo="Saldo do período" valor={fmt(kpis.saldoPeriodo)} icon={Wallet}
            cor={kpis.saldoPeriodo >= 0 ? 'green' : 'red'} />
          <KpiCard titulo="Inadimplência" valor={fmtPct(kpis.inadimplencia)} icon={AlertTriangle}
            cor={kpis.inadimplencia > 5 ? 'red' : kpis.inadimplencia > 0 ? 'yellow' : 'default'}
            detalhe="Receitas vencidas / total a receber" />
        </div>

        {fluxo.length > 0 ? (
          <div className="rounded-lg bg-card border p-4">
            <p className="text-sm font-medium mb-3">Fluxo de caixa por dia</p>
            <ResponsiveContainer width="100%" height={220}>
              <ComposedChart data={fluxo} margin={{ left: 0, right: 4 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                <XAxis dataKey="dia" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false}
                  tickFormatter={v => `R$${((v as number) / 1000).toFixed(0)}k`} />
                <Tooltip
                  formatter={(v: unknown, name: unknown) => [
                    fmt(v as number),
                    name === 'receitas' ? 'Receitas' : name === 'despesas' ? 'Despesas' : 'Saldo',
                  ]}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }}
                />
                <Legend iconType="circle" wrapperStyle={{ fontSize: 11, paddingTop: 4 }} />
                <Bar dataKey="receitas" name="receitas" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
                <Bar dataKey="despesas" name="despesas" fill="hsl(0 72% 51%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
                <Line type="monotone" dataKey="saldo" name="saldo"
                  stroke="hsl(221 83% 53%)" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
              </ComposedChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="rounded-lg bg-card border p-4 flex h-28 items-center justify-center text-sm text-muted-foreground">
            Sem lançamentos pagos no período
          </div>
        )}
      </Panel>

    </div>
  )
}
