import { CreditCard, AlertTriangle, Percent, DollarSign } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioCobrancasResponse } from '@/types/relatorios'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from 'recharts'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioCobrancasResponse }

export default function AbaCobrancas({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <KpiCard titulo="Total a receber" valor={fmt(dados.totalReceber)} icon={CreditCard} cor="green" />
        <KpiCard titulo="Total vencido" valor={fmt(dados.totalVencido)} icon={AlertTriangle}
          cor={dados.totalVencido > 0 ? 'red' : 'default'} />
        <KpiCard titulo="Cobranças vencidas" valor={dados.vencidosCount.toLocaleString('pt-BR')} icon={AlertTriangle}
          cor={dados.vencidosCount > 0 ? 'red' : 'default'} />
        <KpiCard titulo="Inadimplência" valor={`${dados.taxaInadimplencia.toFixed(1)}%`} icon={Percent}
          cor={dados.taxaInadimplencia > 10 ? 'red' : dados.taxaInadimplencia > 5 ? 'yellow' : 'default'} />
      </div>

      {dados.aging.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <p className="text-sm font-medium mb-3">Aging de Cobranças Vencidas</p>
          <ResponsiveContainer width="100%" height={160}>
            <BarChart data={dados.aging} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" className="stroke-border" />
              <XAxis type="number" tick={{ fontSize: 11 }} />
              <YAxis dataKey="faixa" type="category" tick={{ fontSize: 11 }} width={90} />
              <Tooltip formatter={(v: unknown, name: unknown) => [name === 'count' ? v : fmt(v as number), name === 'count' ? 'Qtd' : 'Total']} contentStyle={{ fontSize: 12, borderRadius: 8 }} />
              <Bar dataKey="count" name="count" fill="hsl(0 72% 51%)" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Todas as Cobranças</p>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground">Referência</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Cliente</th>
              <th className="text-right px-4 py-2 font-medium text-muted-foreground">Valor</th>
              <th className="text-left px-4 py-2 font-medium text-muted-foreground">Vencimento</th>
              <th className="text-center px-4 py-2 font-medium text-muted-foreground">Status</th>
            </tr>
          </thead>
          <tbody>
            {dados.cobrancas.map((c, i) => (
              <tr key={i} className="border-t hover:bg-muted/30">
                <td className="px-4 py-2">{c.referencia}</td>
                <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{c.clienteNome}</td>
                <td className="px-4 py-2 text-right font-medium">{fmt(c.valor)}</td>
                <td className="px-4 py-2">{c.dataVencimento}</td>
                <td className="px-4 py-2 text-center">
                  <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium',
                    c.status === 'Pago' ? 'bg-green-100 text-green-700' :
                    c.diasAtraso > 0 ? 'bg-red-100 text-red-700' : 'bg-yellow-100 text-yellow-700'
                  )}>
                    {c.status === 'Pendente' && c.diasAtraso > 0 ? `Vencido ${c.diasAtraso}d` : c.status}
                  </span>
                </td>
              </tr>
            ))}
            {!dados.cobrancas.length && <tr><td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">Nenhuma cobrança</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
