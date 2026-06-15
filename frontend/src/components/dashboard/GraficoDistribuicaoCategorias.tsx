import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { EstoqueCategoriaItem } from '@/types/dashboard'

const COLORS = ['hsl(221 83% 53%)', 'hsl(142 76% 36%)', 'hsl(38 92% 50%)', 'hsl(0 72% 51%)', 'hsl(262 83% 58%)', 'hsl(16 85% 50%)', 'hsl(199 89% 48%)']
const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: EstoqueCategoriaItem[] }

export default function GraficoDistribuicaoCategorias({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Estoque por Categoria</p>
      <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">Sem produtos ativos</div>
    </div>
  )
  const data = dados.map(d => ({ name: d.categoria, value: d.valor, qty: d.quantidade }))
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Estoque por Categoria</p>
      <p className="text-xs text-muted-foreground mb-3">Valor em R$ por categoria</p>
      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie data={data} cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={2} dataKey="value">
            {data.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
          </Pie>
          <Tooltip formatter={(v: unknown, _: unknown, props: unknown) => {
            const p = props as { payload?: { qty: number } }
            return [`${fmt(v)} (${p.payload?.qty ?? 0} SKUs)`, 'Valor']
          }} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
