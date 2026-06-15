import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { ReceitaCategoriaItem } from '@/types/dashboard'

const COLORS = ['hsl(142 76% 36%)', 'hsl(221 83% 53%)', 'hsl(38 92% 50%)', 'hsl(262 83% 58%)', 'hsl(199 89% 48%)', 'hsl(16 85% 50%)']
const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: ReceitaCategoriaItem[] }

export default function GraficoReceitasCategoria({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Receitas por Categoria</p>
      <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">Sem receitas no mês</div>
    </div>
  )
  const data = dados.map(d => ({ name: d.categoria, value: d.total }))
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Receitas por Categoria</p>
      <p className="text-xs text-muted-foreground mb-3">Lançamentos recebidos no mês</p>
      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie data={data} cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={2} dataKey="value">
            {data.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
          </Pie>
          <Tooltip formatter={(v: unknown) => [fmt(v), 'Total']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
