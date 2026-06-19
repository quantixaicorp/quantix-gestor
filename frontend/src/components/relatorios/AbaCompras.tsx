import { useEffect, useState } from 'react'
import {
  LineChart, Line, BarChart, Bar,
  PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer,
} from 'recharts'
import { useComprasDashboard } from '@/hooks/useComprasDashboard'
import { KpiRow } from '@/components/ui/KpiRow'

const COLORS = ['#6366f1', '#8b5cf6', '#a78bfa', '#c4b5fd', '#ddd6fe', '#ede9fe']

function toMoeda(v: number) {
  return v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
}

export default function AbaCompras() {
  const { data, loading, load } = useComprasDashboard()
  const [de, setDe] = useState(() => {
    const d = new Date()
    return new Date(d.getFullYear(), d.getMonth() - 5, 1).toISOString().slice(0, 10)
  })
  const [ate, setAte] = useState(new Date().toISOString().slice(0, 10))

  useEffect(() => { void load(de, ate) }, [de, ate, load])

  if (loading) return <p className="text-muted-foreground">Carregando...</p>

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm font-semibold text-muted-foreground">Período de análise</p>
        <div className="flex gap-2 items-center flex-wrap">
          <input
            type="date"
            value={de}
            onChange={e => setDe(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          />
          <span className="text-muted-foreground text-sm">até</span>
          <input
            type="date"
            value={ate}
            onChange={e => setAte(e.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm"
          />
        </div>
      </div>

      {data && (
        <>
          <KpiRow items={[
            { label: 'Total do Mês', value: toMoeda(data.totalMes) },
            { label: 'Total do Ano', value: toMoeda(data.totalAno) },
            { label: 'Ticket Médio', value: toMoeda(data.ticketMedio) },
            { label: 'Compras no Mês', value: String(data.qtdComprasMes) },
            { label: 'Fornecedores Ativos', value: String(data.fornecedoresAtivos) },
          ]} />

          <div className="rounded-xl border bg-card p-4 md:p-6">
            <p className="text-sm font-semibold mb-4">Evolução Mensal de Compras</p>
            <ResponsiveContainer width="100%" height={220}>
              <LineChart data={data.seriesMensal}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="mes" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} tickFormatter={v => toMoeda(v as number)} width={80} />
                <Tooltip formatter={(v: unknown) => [toMoeda(v as number), 'Total']} />
                <Line type="monotone" dataKey="total" stroke="hsl(var(--primary))" strokeWidth={2} dot={{ r: 4 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {data.porFornecedor.length > 0 && (
              <div className="rounded-xl border bg-card p-4 md:p-6">
                <p className="text-sm font-semibold mb-4">Compras por Fornecedor</p>
                <ResponsiveContainer width="100%" height={220}>
                  <PieChart>
                    <Pie
                      data={data.porFornecedor}
                      dataKey="total"
                      nameKey="fornecedor"
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      label={({ name, percent }: { name?: string; percent?: number }) =>
                        `${name ?? ''} ${((percent ?? 0) * 100).toFixed(0)}%`}
                    >
                      {data.porFornecedor.map((_, i) => (
                        <Cell key={i} fill={COLORS[i % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip formatter={(v: unknown) => toMoeda(v as number)} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            )}

            {data.topProdutos.length > 0 && (
              <div className="rounded-xl border bg-card p-4 md:p-6">
                <p className="text-sm font-semibold mb-4">Top Produtos Comprados</p>
                <ResponsiveContainer width="100%" height={220}>
                  <BarChart data={data.topProdutos.slice(0, 8)} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                    <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={v => toMoeda(v as number)} />
                    <YAxis type="category" dataKey="produto" tick={{ fontSize: 11 }} width={100} />
                    <Tooltip formatter={(v: unknown) => toMoeda(v as number)} />
                    <Bar dataKey="valorTotal" fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
