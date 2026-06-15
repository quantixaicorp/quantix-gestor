import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'
import type { OrcamentoStatusItem } from '@/types/dashboard'

const fmt = (v: unknown) => (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const STATUS_COLOR: Record<string, string> = {
  Enviado: 'hsl(221 83% 53%)',
  Aprovado: 'hsl(142 76% 36%)',
  Convertido: 'hsl(142 60% 25%)',
  Rejeitado: 'hsl(0 72% 51%)',
  Cancelado: 'hsl(0 50% 60%)',
  Expirado: 'hsl(38 92% 50%)',
}

interface Props { dados: OrcamentoStatusItem[] }

export default function GraficoOrcamentosStatus({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium">Orçamentos por Status</p>
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Sem orçamentos</div>
    </div>
  )
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-3">Orçamentos por Status</p>
      <ResponsiveContainer width="100%" height={200}>
        <BarChart data={dados}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
          <XAxis dataKey="status" tick={{ fontSize: 11 }} />
          <YAxis tick={{ fontSize: 11 }} />
          <Tooltip formatter={(v: unknown, name: unknown) => [name === 'count' ? String(v as number) : fmt(v), name === 'count' ? 'Qtd' : 'Valor']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Bar dataKey="count" name="count" radius={[4, 4, 0, 0]}>
            {dados.map((d, i) => <Cell key={i} fill={STATUS_COLOR[d.status] ?? `hsl(${i * 50} 70% 50%)`} />)}
          </Bar>
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
