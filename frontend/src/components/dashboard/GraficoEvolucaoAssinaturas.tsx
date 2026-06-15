import {
  ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import type { EvolucaoAssinaturaMes } from '@/types/dashboard'

interface Props { dados: EvolucaoAssinaturaMes[] }

export default function GraficoEvolucaoAssinaturas({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium">Evolução de Assinaturas</p>
      <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">Sem dados</div>
    </div>
  )
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Evolução de Assinaturas — 12 meses</p>
      <p className="text-xs text-muted-foreground mb-4">Ativas, novas e canceladas por mês</p>
      <ResponsiveContainer width="100%" height={220}>
        <ComposedChart data={dados} margin={{ left: 0, right: 8 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
          <XAxis dataKey="mes" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
          <YAxis tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
          <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12, paddingTop: 8 }} />
          <Bar dataKey="novas" name="Novas" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
          <Bar dataKey="canceladas" name="Canceladas" fill="hsl(0 72% 51%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
          <Line type="monotone" dataKey="ativas" name="Ativas" stroke="hsl(221 83% 53%)" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
