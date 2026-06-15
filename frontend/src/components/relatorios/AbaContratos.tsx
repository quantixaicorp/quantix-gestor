import { FileText, TrendingUp, Bell } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioContratosResponse } from '@/types/relatorios'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioContratosResponse }

export default function AbaContratos({ dados }: Props) {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        <KpiCard titulo="Contratos ativos" valor={dados.totalAtivos.toLocaleString('pt-BR')} icon={FileText} />
        <KpiCard titulo="MRR total" valor={fmt(dados.mrrTotal)} icon={TrendingUp} cor="green" />
        <KpiCard titulo="Vencendo em 30 dias" valor={dados.vencendoEm30.toLocaleString('pt-BR')} icon={Bell}
          cor={dados.vencendoEm30 > 0 ? 'yellow' : 'default'} />
      </div>

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Todos os Contratos Ativos</p>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Título</th>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden sm:table-cell">Cliente</th>
              <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Valor</th>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden md:table-cell">Periodicidade</th>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden lg:table-cell">Vencimento</th>
              <th className="text-center px-4 py-2.5 font-medium text-muted-foreground">Status</th>
            </tr>
          </thead>
          <tbody>
            {dados.contratos.map((c, i) => (
              <tr key={i} className="border-t hover:bg-muted/30">
                <td className="px-4 py-2.5 font-medium">{c.titulo}</td>
                <td className="px-4 py-2.5 hidden sm:table-cell text-muted-foreground">{c.clienteNome}</td>
                <td className="px-4 py-2.5 text-right font-medium">{fmt(c.valor)}</td>
                <td className="px-4 py-2.5 hidden md:table-cell text-muted-foreground">{c.periodicidade}</td>
                <td className="px-4 py-2.5 hidden lg:table-cell text-muted-foreground">{c.dataFim ?? '—'}</td>
                <td className="px-4 py-2.5 text-center">
                  <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium',
                    c.status === 'Ativo' ? 'bg-green-100 text-green-700' : 'bg-muted text-muted-foreground'
                  )}>{c.status}</span>
                </td>
              </tr>
            ))}
            {!dados.contratos.length && <tr><td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">Nenhum contrato ativo</td></tr>}
          </tbody>
        </table>
      </div>
    </div>
  )
}
