import { ClipboardList, Percent, TrendingUp } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioOrcamentosResponse } from '@/types/relatorios'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid, Cell } from 'recharts'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const STATUS_COLOR: Record<string, string> = {
  Enviado: 'hsl(221 83% 53%)', Aprovado: 'hsl(142 76% 36%)', Convertido: 'hsl(142 60% 25%)',
  Rejeitado: 'hsl(0 72% 51%)', Cancelado: 'hsl(0 50% 60%)', Expirado: 'hsl(38 92% 50%)',
}

interface Props { dados: RelatorioOrcamentosResponse }

export default function AbaOrcamentos({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <KpiCard titulo="Total no período" valor={dados.totalNoPeriodo.toLocaleString('pt-BR')} icon={ClipboardList} />
        <KpiCard titulo="Taxa de conversão" valor={`${dados.taxaConversao.toFixed(1)}%`} icon={Percent}
          cor={dados.taxaConversao >= 50 ? 'green' : 'yellow'} />
        <KpiCard titulo="Pipeline em aberto" valor={fmt(dados.valorPipeline)} icon={TrendingUp} />
      </div>

      {dados.porStatus.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Orçamentos por Status</p>
          <ResponsiveContainer width="100%" height={180}>
            <BarChart data={dados.porStatus}>
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis dataKey="status" tick={{ fontSize: 11 }} />
              <YAxis tick={{ fontSize: 11 }} />
              <Tooltip formatter={(v: unknown, name: unknown) => [name === 'count' ? String(v as number) : fmt(v as number), name === 'count' ? 'Qtd' : 'Valor']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Bar dataKey="count" name="count" radius={[4, 4, 0, 0]}>
                {dados.porStatus.map((d, i) => <Cell key={i} fill={STATUS_COLOR[d.status] ?? `hsl(${i * 50} 70% 50%)`} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Orçamentos do Período</p>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground">#</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground">Título</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Cliente</th>
              <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
              <th className="text-center px-4 py-2 font-medium text-muted-foreground">Status</th>
            </tr>
          </thead>
          <tbody>
            {dados.orcamentos.map((o, i) => (
              <tr key={i} className="border-t hover:bg-muted/30">
                <td className="px-4 py-2 text-muted-foreground">#{o.numero}</td>
                <td className="px-4 py-2 font-medium">{o.titulo}</td>
                <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{o.clienteNome}</td>
                <td className="px-4 py-2 text-right font-medium">{fmt(o.valorTotal)}</td>
                <td className="px-4 py-2 text-center">
                  <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium',
                    o.status === 'Convertido' ? 'bg-green-100 text-green-700' :
                    o.status === 'Aprovado' ? 'bg-blue-100 text-blue-700' :
                    o.status === 'Rejeitado' || o.status === 'Cancelado' ? 'bg-red-100 text-red-700' :
                    'bg-muted text-muted-foreground'
                  )}>{o.status}</span>
                </td>
              </tr>
            ))}
            {!dados.orcamentos.length && <tr><td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">Nenhum orçamento no período</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
