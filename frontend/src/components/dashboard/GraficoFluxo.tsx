import {
  LineChart, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from 'recharts'
import type { FluxoDiaResponse } from '@/types/dashboard'

interface Props { dados: FluxoDiaResponse[] }

export default function GraficoFluxo({ dados }: Props) {
  const data = dados.map(d => ({
    dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
    receitas: d.receitas,
    despesas: d.despesas,
  }))

  const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-4">Entradas vs Saídas — mês atual</p>
      <ResponsiveContainer width="100%" height={200}>
        <LineChart data={data}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="dia" tick={{ fontSize: 12 }} />
          <YAxis tick={{ fontSize: 12 }} tickFormatter={fmt} />
          <Tooltip formatter={fmt} />
          <Legend />
          <Line type="monotone" dataKey="receitas" name="Entradas"
            stroke="hsl(142 76% 36%)" strokeWidth={2} dot={false} />
          <Line type="monotone" dataKey="despesas" name="Saídas"
            stroke="hsl(0 72% 51%)" strokeWidth={2} dot={false} />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
