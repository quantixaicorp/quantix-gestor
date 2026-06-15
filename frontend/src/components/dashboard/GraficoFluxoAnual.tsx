import {
  ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer, ReferenceLine,
} from 'recharts'
import type { FluxoMensalItem } from '@/types/dashboard'

const fmtBRL = (v: unknown) =>
  v == null ? '' : (v as number).toLocaleString('pt-BR', { notation: 'compact', style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })
const fmtFull = (v: unknown) =>
  (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: FluxoMensalItem[] }

export default function GraficoFluxoAnual({ dados }: Props) {
  if (!dados?.length) return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Fluxo Anual</p>
      <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">Sem dados</div>
    </div>
  )
  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Fluxo Anual — 12 meses</p>
      <p className="text-xs text-muted-foreground mb-4">Receitas, despesas e saldo acumulado</p>
      <ResponsiveContainer width="100%" height={240}>
        <ComposedChart data={dados} margin={{ left: 0, right: 8 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
          <XAxis dataKey="mes" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
          <YAxis yAxisId="bars" tick={{ fontSize: 11 }} tickFormatter={fmtBRL} tickLine={false} axisLine={false} width={68} />
          <YAxis yAxisId="saldo" orientation="right" tick={{ fontSize: 11 }} tickFormatter={fmtBRL} tickLine={false} axisLine={false} width={68} />
          <Tooltip formatter={(v: unknown, name: unknown) => [fmtFull(v), name === 'receitas' ? 'Receitas' : name === 'despesas' ? 'Despesas' : 'Saldo']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 12, paddingTop: 8 }} />
          <ReferenceLine yAxisId="saldo" y={0} stroke="hsl(var(--muted-foreground))" strokeDasharray="4 4" />
          <Bar yAxisId="bars" dataKey="receitas" name="receitas" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
          <Bar yAxisId="bars" dataKey="despesas" name="despesas" fill="hsl(0 72% 51%)" radius={[3, 3, 0, 0]} maxBarSize={20} />
          <Line yAxisId="saldo" type="monotone" dataKey="saldo" name="saldo" stroke="hsl(221 83% 53%)" strokeWidth={2} dot={false} activeDot={{ r: 4 }} />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
