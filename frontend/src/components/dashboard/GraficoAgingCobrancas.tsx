import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import type { AgingFaixa } from '@/types/dashboard'

const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const COLORS = ['hsl(38 92% 50%)', 'hsl(16 85% 50%)', 'hsl(0 72% 51%)', 'hsl(0 72% 35%)']

interface Props { dados: AgingFaixa[] }

export default function GraficoAgingCobrancas({ dados }: Props) {
  if (!dados?.length || dados.every(d => d.count === 0)) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium">Aging de Cobranças</p>
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Sem cobranças vencidas</div>
    </div>
  )
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-3">Aging de Cobranças Vencidas</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={dados} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis type="number" tick={{ fontSize: 11 }} tickFormatter={v => `${v}`} />
          <YAxis dataKey="faixa" type="category" tick={{ fontSize: 11 }} width={90} />
          <Tooltip formatter={(v: unknown, name: unknown) => [name === 'count' ? String(v as number) : fmt(v), name === 'count' ? 'Qtd' : 'Total']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Bar dataKey="count" name="count" fill={COLORS[0]} radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
