import { Repeat, TrendingUp, XCircle, Percent } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioAssinaturasResponse } from '@/types/relatorios'
import { ComposedChart, Bar, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Legend } from 'recharts'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioAssinaturasResponse }

export default function AbaAssinaturas({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <KpiCard titulo="Assinaturas ativas" valor={dados.totalAtivas.toLocaleString('pt-BR')} icon={Repeat} />
        <KpiCard titulo="MRR total" valor={fmt(dados.mrrTotal)} icon={TrendingUp} cor="green" />
        <KpiCard titulo="Canceladas no período" valor={dados.canceladasNoPeriodo.toLocaleString('pt-BR')} icon={XCircle}
          cor={dados.canceladasNoPeriodo > 0 ? 'red' : 'default'} />
        <KpiCard titulo="Taxa de churn" valor={`${dados.taxaChurn.toFixed(1)}%`} icon={Percent}
          cor={dados.taxaChurn > 5 ? 'red' : dados.taxaChurn > 2 ? 'yellow' : 'default'} />
      </div>

      {dados.evolucao.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-1">Evolução de Assinaturas</p>
          <p className="text-xs text-muted-foreground mb-4">Ativas, novas e canceladas por mês</p>
          <ResponsiveContainer width="100%" height={220}>
            <ComposedChart data={dados.evolucao} margin={{ left: 0, right: 8 }}>
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
      )}

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Assinaturas Ativas</p>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground">Cliente</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Plano</th>
              <th className="text-right px-4 py-2 font-medium text-muted-foreground">Valor</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden md:table-cell">Periodicidade</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden lg:table-cell">Renovação</th>
              <th className="text-center px-4 py-2 font-medium text-muted-foreground">Status</th>
            </tr>
          </thead>
          <tbody>
            {dados.assinaturas.map((a, i) => (
              <tr key={i} className="border-t hover:bg-muted/30">
                <td className="px-4 py-2 font-medium">{a.clienteNome}</td>
                <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{a.plano}</td>
                <td className="px-4 py-2 text-right font-medium">{fmt(a.valor)}</td>
                <td className="px-4 py-2 hidden md:table-cell text-muted-foreground">{a.periodicidade}</td>
                <td className="px-4 py-2 hidden lg:table-cell text-muted-foreground">{a.dataRenovacao}</td>
                <td className="px-4 py-2 text-center">
                  <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium',
                    a.status === 'Ativo' ? 'bg-green-100 text-green-700' :
                    a.status === 'Cancelado' ? 'bg-red-100 text-red-700' : 'bg-muted text-muted-foreground'
                  )}>{a.status}</span>
                </td>
              </tr>
            ))}
            {!dados.assinaturas.length && <tr><td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">Nenhuma assinatura</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
