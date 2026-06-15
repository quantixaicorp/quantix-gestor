import { Calendar, CheckCircle, XCircle, Percent } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioAgendamentosResponse } from '@/types/relatorios'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'

const STATUS_COLOR: Record<string, string> = {
  Concluido: 'hsl(142 76% 36%)',
  Confirmado: 'hsl(221 83% 53%)',
  Agendado: 'hsl(38 92% 50%)',
  AguardandoConfirmacao: 'hsl(199 89% 48%)',
  Cancelado: 'hsl(0 72% 51%)',
}

interface Props { dados: RelatorioAgendamentosResponse }

export default function AbaAgendamentos({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <KpiCard titulo="Total no período" valor={dados.totalNoPeriodo.toLocaleString('pt-BR')} icon={Calendar} />
        <KpiCard titulo="Concluídos" valor={dados.concluidos.toLocaleString('pt-BR')} icon={CheckCircle} cor="green" />
        <KpiCard titulo="Cancelados" valor={dados.cancelados.toLocaleString('pt-BR')} icon={XCircle}
          cor={dados.cancelados > 0 ? 'red' : 'default'} />
        <KpiCard titulo="Taxa de conclusão" valor={`${dados.taxaConclusao.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`}
          icon={Percent} cor={dados.taxaConclusao >= 70 ? 'green' : 'yellow'} />
        <KpiCard titulo="Taxa de ocupação" valor={`${dados.taxaOcupacao.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%`}
          icon={Percent} cor={dados.taxaOcupacao >= 70 ? 'green' : 'yellow'} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Por Status</p>
          {dados.porStatus.length > 0 ? (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={dados.porStatus}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
                <XAxis dataKey="status" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip contentStyle={{ fontSize: 12, borderRadius: 8 }} />
                <Bar dataKey="count" name="Quantidade" radius={[4, 4, 0, 0]}>
                  {dados.porStatus.map((d, i) => <Cell key={i} fill={STATUS_COLOR[d.status] ?? `hsl(${i * 60} 70% 50%)`} />)}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          ) : <p className="text-sm text-muted-foreground py-8 text-center">Sem dados</p>}
        </div>

        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Por Profissional</p>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead><tr className="border-b">
                <th className="py-2 text-left font-medium text-muted-foreground">Profissional</th>
                <th className="py-2 text-right font-medium text-muted-foreground hidden sm:table-cell">Total</th>
                <th className="py-2 text-right font-medium text-muted-foreground">Concluídos</th>
                <th className="py-2 text-right font-medium text-muted-foreground hidden sm:table-cell">Taxa</th>
              </tr></thead>
              <tbody>
                {dados.porProfissional.map((p, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <td className="py-2">{p.profissional}</td>
                    <td className="py-2 text-right hidden sm:table-cell">{p.total}</td>
                    <td className="py-2 text-right text-green-600">{p.concluidos}</td>
                    <td className="py-2 text-right hidden sm:table-cell">{p.taxaConclusao.toFixed(1)}%</td>
                  </tr>
                ))}
                {!dados.porProfissional.length && <tr><td colSpan={4} className="py-4 text-center text-muted-foreground">Sem dados</td></tr>}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  )
}
