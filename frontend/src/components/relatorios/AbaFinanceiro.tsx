import { useState } from 'react'
import {
  ComposedChart, Bar, Line, XAxis, YAxis,
  CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  PieChart, Pie, Cell,
} from 'recharts'
import { ArrowUpCircle, ArrowDownCircle, Wallet } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioFinanceiroResponse } from '@/types/relatorios'

const CORES = ['hsl(0 72% 51%)', 'hsl(38 92% 50%)', 'hsl(262 80% 50%)', 'hsl(142 76% 36%)', 'hsl(200 98% 39%)']
const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

interface Props {
  dados: RelatorioFinanceiroResponse
  tipoData: string
  onChangeTipoData: (v: string) => void
}

function Panel({ titulo, children }: { titulo: string; children: React.ReactNode }) {
  return (
    <div className="rounded-xl border bg-muted/20 p-3 sm:p-5 space-y-4">
      <p className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{titulo}</p>
      {children}
    </div>
  )
}

export default function AbaFinanceiro({ dados, tipoData, onChangeTipoData }: Props) {
  const [filtroTipo, setFiltroTipo] = useState('')
  const [filtroCategoria, setFiltroCategoria] = useState('')
  const categorias = [...new Set(dados.analitico.map(l => l.categoria))].sort()
  const fluxo = dados.fluxoPorDia.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
    saldo: d.saldo,
  }))

  const analiticoFiltrado = dados.analitico.filter(l =>
    (!filtroTipo || l.tipo === filtroTipo) &&
    (!filtroCategoria || l.categoria === filtroCategoria)
  )

  return (
    <div className="space-y-5">

      {/* ── Filtro de tipo de data ─────────────────────────── */}
      <div className="flex flex-wrap gap-2 items-center">
        <span className="text-sm text-muted-foreground">Filtrar por:</span>
        {(['pagamento', 'vencimento'] as const).map(v => (
          <button key={v} onClick={() => onChangeTipoData(v)}
            className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
              tipoData === v
                ? 'bg-primary text-primary-foreground border-primary'
                : 'border-input text-muted-foreground hover:text-foreground'
            }`}>
            Data de {v === 'pagamento' ? 'pagamento' : 'vencimento'}
          </button>
        ))}
      </div>

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

      {/* ── Tabela Analítica ───────────────────────────────── */}
      {dados.analitico.length > 0 && (
        <Panel titulo="Tabela Analítica">
          <div className="flex flex-wrap gap-2 mb-3">
            <select value={filtroTipo} onChange={e => setFiltroTipo(e.target.value)}
              className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
              <option value="">Todos os tipos</option>
              <option value="Receita">Receita</option>
              <option value="Despesa">Despesa</option>
            </select>
            <select value={filtroCategoria} onChange={e => setFiltroCategoria(e.target.value)}
              className="h-8 rounded-md border border-input bg-transparent px-3 text-sm">
              <option value="">Todas as categorias</option>
              {categorias.map(c => <option key={c} value={c}>{c}</option>)}
            </select>
            {(filtroTipo || filtroCategoria) && (
              <button onClick={() => { setFiltroTipo(''); setFiltroCategoria('') }}
                className="h-8 px-3 text-xs text-muted-foreground hover:text-foreground rounded-md border border-input">
                Limpar
              </button>
            )}
          </div>
          <div className="overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-3 py-2 text-left font-medium">Descrição</th>
                  <th className="px-3 py-2 text-left font-medium">Tipo</th>
                  <th className="px-3 py-2 text-left font-medium">Categoria</th>
                  <th className="px-3 py-2 text-left font-medium">Vencimento</th>
                  <th className="px-3 py-2 text-left font-medium">Pagamento</th>
                  <th className="px-3 py-2 text-right font-medium">Valor</th>
                  <th className="px-3 py-2 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {analiticoFiltrado.map(l => (
                  <tr key={l.id} className="border-b hover:bg-muted/20">
                    <td className="px-3 py-2">{l.descricao}</td>
                    <td className="px-3 py-2">
                      <Badge variant={l.tipo === 'Receita' ? 'secondary' : 'destructive'} className="text-xs">{l.tipo}</Badge>
                    </td>
                    <td className="px-3 py-2 text-muted-foreground">{l.categoria}</td>
                    <td className="px-3 py-2 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                    <td className="px-3 py-2 text-muted-foreground">{l.dataPagamento ? fmtDate(l.dataPagamento) : '—'}</td>
                    <td className="px-3 py-2 text-right font-medium">{fmt(l.valor)}</td>
                    <td className="px-3 py-2">
                      <Badge variant={l.status === 'Pago' ? 'secondary' : l.status === 'Cancelado' ? 'outline' : 'outline'} className="text-xs">
                        {l.status}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
              <tfoot>
                <tr className="bg-muted/30 font-medium">
                  <td colSpan={5} className="px-3 py-2 text-sm">Total filtrado</td>
                  <td className="px-3 py-2 text-right">
                    {fmt(analiticoFiltrado.reduce((s, l) => s + (l.tipo === 'Receita' ? l.valor : -l.valor), 0))}
                  </td>
                  <td />
                </tr>
              </tfoot>
            </table>
          </div>
        </Panel>
      )}

    </div>
  )
}
