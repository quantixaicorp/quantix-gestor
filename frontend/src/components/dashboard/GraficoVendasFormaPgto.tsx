import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { VendaFormaPgtoItem } from '@/types/dashboard'

const COLORS = ['hsl(221 83% 53%)', 'hsl(142 76% 36%)', 'hsl(38 92% 50%)', 'hsl(0 72% 51%)', 'hsl(262 83% 58%)']
const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: VendaFormaPgtoItem[] }

export default function GraficoVendasFormaPgto({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Vendas por Forma de Pagamento</p>
      <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">Sem vendas no mês</div>
    </div>
  )
  const data = dados.map(d => ({ name: d.formaPagamento, value: d.total, qtd: d.quantidade }))
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Vendas por Forma de Pagamento</p>
      <p className="text-xs text-muted-foreground mb-3">Distribuição do mês atual</p>
      <ResponsiveContainer width="100%" height={220}>
        <PieChart>
          <Pie data={data} cx="50%" cy="50%" innerRadius={50} outerRadius={80} paddingAngle={2} dataKey="value">
            {data.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
          </Pie>
          <Tooltip formatter={(v: unknown, _: unknown, props: unknown) => {
            const p = props as { payload?: { qtd: number } }
            return [`${fmt(v)} (${p.payload?.qtd ?? 0} vendas)`, 'Total']
          }} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
