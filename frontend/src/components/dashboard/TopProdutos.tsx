import {
  BarChart, Bar, XAxis, YAxis, Tooltip,
  ResponsiveContainer, CartesianGrid
} from 'recharts'
import type { TopProdutoResponse } from '@/types/dashboard'

interface Props { dados: TopProdutoResponse[] }

export default function TopProdutos({ dados }: Props) {
  const data = (dados ?? []).map(d => ({
    nome: d.nome.length > 15 ? d.nome.slice(0, 15) + '…' : d.nome,
    qtd: d.quantidadeVendida,
    total: d.totalFaturado,
  }))

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Top 5 produtos mais vendidos</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data} layout="vertical">
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis type="number" tick={{ fontSize: 12 }} />
          <YAxis dataKey="nome" type="category" tick={{ fontSize: 11 }} width={120} />
          <Tooltip
            formatter={(v: unknown) => [String(v), 'Unidades vendidas']} />
          <Bar dataKey="qtd" fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
