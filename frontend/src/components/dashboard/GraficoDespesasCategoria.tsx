import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import type { CategoriaDespesaDashResponse } from '@/types/dashboard'

interface Props { dados: CategoriaDespesaDashResponse[] }

const COLORS = [
  'hsl(221 83% 53%)', 'hsl(0 72% 51%)', 'hsl(142 76% 36%)',
  'hsl(38 92% 50%)', 'hsl(262 83% 58%)', 'hsl(199 89% 48%)',
  'hsl(336 80% 58%)', 'hsl(16 85% 50%)',
]

const fmt = (v: unknown) =>
  (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function GraficoDespesasCategoria({ dados }: Props) {
  if (!dados || dados.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-1">Despesas por Categoria</p>
        <p className="text-xs text-muted-foreground mb-4">Top categorias de gasto no mês</p>
        <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">
          Nenhuma despesa registrada este mês
        </div>
      </div>
    )
  }

  const data = dados.map(d => ({ name: d.categoria, value: d.total }))

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Despesas por Categoria</p>
      <p className="text-xs text-muted-foreground mb-4">Top categorias de gasto no mês</p>
      <ResponsiveContainer width="100%" height={240}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={60}
            outerRadius={90}
            paddingAngle={2}
            dataKey="value"
          >
            {data.map((_, i) => (
              <Cell key={i} fill={COLORS[i % COLORS.length]} />
            ))}
          </Pie>
          <Tooltip formatter={(v: unknown) => [fmt(v), 'Total']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
