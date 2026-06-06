import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer
} from 'recharts'
import type { VendasDiaResponse } from '@/types/dashboard'

interface Props { dados: VendasDiaResponse[] }

export default function GraficoVendas({ dados }: Props) {
  const data = (dados ?? []).map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    total: d.total,
    qtd: d.quantidade,
  }))

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Vendas — últimos 7 dias</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }}
            tickFormatter={v => v == null ? '' : (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })} />
          <Tooltip
            formatter={(v: unknown) => [v == null ? '' : (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }), 'Total']} />
          <Bar dataKey="total" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
