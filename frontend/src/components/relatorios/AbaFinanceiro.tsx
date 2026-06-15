import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell,
} from 'recharts'
import { ArrowUpCircle, ArrowDownCircle, Wallet } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioFinanceiroResponse } from '@/types/relatorios'

const CORES = ['hsl(0 72% 51%)', 'hsl(38 92% 50%)', 'hsl(262 80% 50%)', 'hsl(142 76% 36%)', 'hsl(200 98% 39%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioFinanceiroResponse }

function Panel({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border bg-muted/20 p-3 sm:p-5 space-y-4">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{titulo}</p>
      {children}
    </div>
  )
}

export default function AbaFinanceiro({ dados }: Props) {
  const fluxo = dados.fluxoPorDia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
    saldo: d.saldo,
  }))

  return (
    <div className="space-y-5">

      {/* ── Resumo + Fluxo ─────────────────────────────────── */}
      <Panel titulo="Fluxo de Caixa">
        <div className="grid grid-cols-3 gap-3">
          <KpiCard titulo="Total Receitas" valor={fmt(dados.totalReceitas)} icon={ArrowUpCircle} cor="green" />
          <KpiCard titulo="Total Despesas" valor={fmt(dados.totalDespesas)} icon={ArrowDownCircle}
            cor={dados.totalDespesas > 0 ? 'red' : 'default'} />
          <KpiCard titulo="Saldo" valor={fmt(dados.saldo)} icon={Wallet}
            cor={dados.saldo >= 0 ? 'green' : 'red'} />
        </div>

        {fluxo.length > 0 ? (
          <div className="rounded-lg bg-card border p-4">
            <p className="text-sm font-medium mb-3">Fluxo por dia</p>
            <ResponsiveContainer width="100%" height={240}>
              <ComposedChart data={fluxo} margin={{ left: 0, right: 4 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
                <XAxis dataKey="dia" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
                <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false}
                  tickFormatter={v => `R$${((v as number) / 1000).toFixed(0)}k`} />
                <Tooltip formatter={(v: unknown) => [fmt(v as number)]}
                  contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                <Legend iconType="circle" wrapperStyle={{ fontSize: 11, paddingTop: 4 }} />
                <Bar dataKey="receitas" name="Receitas" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} maxBarSize={18} />
                <Bar dataKey="despesas" name="Despesas" fill="hsl(0 72% 51%)" radius={[3, 3, 0, 0]} maxBarSize={18} />
                <Line type="monotone" dataKey="saldo" name="Saldo"
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

      {/* ── Despesas por Categoria ─────────────────────────── */}
      {dados.categoriasDespesas.length > 0 && (
        <Panel titulo="Despesas por Categoria">
          <div className="rounded-lg bg-card border p-4">
            <div className="grid lg:grid-cols-2 gap-4 items-center">
              <ResponsiveContainer width="100%" height={200}>
                <PieChart>
                  <Pie data={dados.categoriasDespesas} dataKey="total"
                    nameKey="categoria" cx="50%" cy="50%" outerRadius={80}>
                    {dados.categoriasDespesas.map((_e, i) => (
                      <Cell key={i} fill={CORES[i % CORES.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(v: unknown) => [fmt(v as number)]}
                    contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                </PieChart>
              </ResponsiveContainer>
              <table className="text-sm w-full">
                <tbody>
                  {dados.categoriasDespesas.map((c, i) => (
                    <tr key={i} className="border-b last:border-0">
                      <td className="py-2 flex items-center gap-2">
                        <span className="w-3 h-3 rounded-full shrink-0"
                          style={{ background: CORES[i % CORES.length] }} />
                        {c.categoria}
                      </td>
                      <td className="py-2 text-right font-medium">{fmt(c.total)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </Panel>
      )}

    </div>
  )
}
