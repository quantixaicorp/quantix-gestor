import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Legend } from 'recharts'
import type { AgendamentoProfissionalItem } from '@/types/dashboard'

interface Props { dados: AgendamentoProfissionalItem[] }

export default function GraficoOcupacaoProfissional({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium">Ocupação por Profissional</p>
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Sem dados do mês</div>
    </div>
  )
  const data = dados.map(d => ({
    nome: d.profissional.length > 12 ? d.profissional.slice(0, 12) + '…' : d.profissional,
    total: d.total,
    concluidos: d.concluidos,
  }))
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-3">Ocupação por Profissional — mês</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={data}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="nome" tick={{ fontSize: 11 }} />
          <YAxis tick={{ fontSize: 11 }} />
          <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
          <Bar dataKey="total" name="Total" fill="hsl(221 83% 53%)" radius={[3, 3, 0, 0]} />
          <Bar dataKey="concluidos" name="Concluídos" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
