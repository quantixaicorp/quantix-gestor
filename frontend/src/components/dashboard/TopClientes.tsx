import {
  BarChart, Bar, XAxis, YAxis, Tooltip,
  ResponsiveContainer, CartesianGrid
} from 'recharts'
import type { TopClienteResponse } from '@/types/dashboard'

interface Props { dados: TopClienteResponse[] }

const fmt = (v: unknown) =>
  (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function TopClientes({ dados }: Props) {
  const data = (dados ?? []).map(d => ({
    nome: d.nome.length > 15 ? d.nome.slice(0, 15) + '…' : d.nome,
    total: d.totalGasto,
  }))

  if (data.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-4">Top clientes por faturamento</p>
        <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">
          Nenhuma venda registrada este mês
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Top clientes por faturamento</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis type="number" tick={{ fontSize: 12 }} tickFormatter={v => fmt(v)} />
          <YAxis dataKey="nome" type="category" tick={{ fontSize: 11 }} width={120} />
          <Tooltip formatter={(v: unknown) => [fmt(v), 'Total comprado']} />
          <Bar dataKey="total" fill="hsl(142 76% 36%)" radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
