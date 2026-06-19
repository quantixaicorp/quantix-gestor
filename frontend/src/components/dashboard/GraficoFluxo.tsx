import { useEffect, useState } from 'react'
import {
  ComposedChart, Bar, Line, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer, ReferenceLine,
} from 'recharts'
import type { FluxoDiaResponse } from '@/types/dashboard'

interface Props { dados: FluxoDiaResponse[] }

const fmtBRL = (v: unknown) =>
  v == null ? '' : (v as number).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

function useIsMobile() {
  const [mobile, setMobile] = useState(() => window.innerWidth < 768)
  useEffect(() => {
    const handler = () => setMobile(window.innerWidth < 768)
    window.addEventListener('resize', handler)
    return () => window.removeEventListener('resize', handler)
  }, [])
  return mobile
}

export default function GraficoFluxo({ dados }: Props) {
  const isMobile = useIsMobile()
  let saldoAcum = 0
  const data = (dados ?? []).map(d => {
    saldoAcum += d.receitas - d.despesas
    return {
      dia: new Date(d.data).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }),
      receitas: d.receitas,
      despesas: d.despesas,
      saldo: saldoAcum,
    }
  })

  if (data.length === 0) {
    return (
      <div className="rounded-lg border bg-card p-4">
        <p className="text-sm font-medium mb-1">Fluxo de Caixa — mês atual</p>
        <p className="text-xs text-muted-foreground mb-4">Entradas, saídas e saldo acumulado</p>
        <div className="flex h-48 items-center justify-center text-sm text-muted-foreground">
          Nenhum lançamento registrado este mês
        </div>
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card p-4">
      <p className="text-sm font-medium mb-1">Fluxo de Caixa — mês atual</p>
      <p className="text-xs text-muted-foreground mb-4">Entradas, saídas e saldo acumulado</p>
      <ResponsiveContainer width="100%" height={240}>
        <ComposedChart data={data} margin={{ top: 4, right: isMobile ? 4 : 8, left: 0, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
          <XAxis dataKey="dia" tick={{ fontSize: 10 }} tickLine={false} axisLine={false} interval={isMobile ? 'preserveStartEnd' : 0} />
          <YAxis
            yAxisId="bars"
            tick={{ fontSize: 10 }}
            tickFormatter={v => (v as number).toLocaleString('pt-BR', { notation: 'compact', style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })}
            tickLine={false}
            axisLine={false}
            width={isMobile ? 52 : 68}
          />
          {!isMobile && (
            <YAxis
              yAxisId="saldo"
              orientation="right"
              tick={{ fontSize: 10 }}
              tickFormatter={v => (v as number).toLocaleString('pt-BR', { notation: 'compact', style: 'currency', currency: 'BRL', maximumFractionDigits: 0 })}
              tickLine={false}
              axisLine={false}
              width={68}
            />
          )}
          <Tooltip
            formatter={(value, name) => [fmtBRL(value), name]}
            contentStyle={{ fontSize: 12, borderRadius: 8 }}
          />
          <Legend iconType="circle" wrapperStyle={{ fontSize: 11, paddingTop: 8 }} />
          <ReferenceLine yAxisId={isMobile ? 'bars' : 'saldo'} y={0} stroke="hsl(var(--muted-foreground))" strokeDasharray="4 4" />
          <Bar yAxisId="bars" dataKey="receitas" name="Entradas" fill="hsl(142 76% 36%)" radius={[3, 3, 0, 0]} maxBarSize={28} />
          <Bar yAxisId="bars" dataKey="despesas" name="Saídas" fill="hsl(0 72% 51%)" radius={[3, 3, 0, 0]} maxBarSize={28} />
          <Line
            yAxisId={isMobile ? 'bars' : 'saldo'}
            type="monotone"
            dataKey="saldo"
            name="Saldo acumulado"
            stroke="hsl(221 83% 53%)"
            strokeWidth={2}
            dot={false}
            activeDot={{ r: 4 }}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  )
}
