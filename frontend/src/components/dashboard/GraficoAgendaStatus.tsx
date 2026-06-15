import { PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer } from 'recharts'
import type { AgendamentoStatusItem } from '@/types/dashboard'

const STATUS_COLOR: Record<string, string> = {
  Concluido: 'hsl(142 76% 36%)',
  Confirmado: 'hsl(221 83% 53%)',
  Agendado: 'hsl(38 92% 50%)',
  AguardandoConfirmacao: 'hsl(199 89% 48%)',
  Cancelado: 'hsl(0 72% 51%)',
}

interface Props { dados: AgendamentoStatusItem[] }

export default function GraficoAgendaStatus({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium">Agendamentos por Status</p>
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Sem agendamentos hoje</div>
    </div>
  )
  const data = dados.map(d => ({ name: d.status, value: d.count }))
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-3">Agendamentos por Status — hoje</p>
      <ResponsiveContainer width="100%" height={200}>
        <PieChart>
          <Pie data={data} cx="50%" cy="50%" outerRadius={75} dataKey="value">
            {data.map((d, i) => <Cell key={i} fill={STATUS_COLOR[d.name] ?? `hsl(${i * 60} 70% 50%)`} />)}
          </Pie>
          <Tooltip formatter={(v: unknown) => [v, 'Qtd']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  )
}
