import type { AgendamentoDoDiaItem } from '@/types/dashboard'
import { cn } from '@/lib/utils'

const STATUS_COR: Record<string, string> = {
  Concluido: 'bg-green-100 text-green-700 dark:bg-green-950/30 dark:text-green-400',
  Confirmado: 'bg-blue-100 text-blue-700 dark:bg-blue-950/30 dark:text-blue-400',
  Agendado: 'bg-muted text-muted-foreground',
  AguardandoConfirmacao: 'bg-yellow-100 text-yellow-700',
  Cancelado: 'bg-red-100 text-red-700',
}

interface Props { dados: AgendamentoDoDiaItem[] }

export default function TabelaAgendaHoje({ dados }: Props) {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <div className="px-4 py-3 border-b bg-muted/30 flex items-center justify-between">
        <p className="text-sm font-semibold">Agenda de Hoje</p>
        <span className="text-xs text-muted-foreground">{dados?.length ?? 0} agendamento(s)</span>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground w-16">Hora</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Cliente</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Serviço</th>
            <th className="text-center px-4 py-2 font-medium text-muted-foreground">Status</th>
          </tr>
        </thead>
        <tbody>
          {(dados ?? []).map((a, i) => (
            <tr key={i} className="border-t hover:bg-muted/20">
              <td className="px-4 py-2 font-mono text-sm">{a.horaInicio}</td>
              <td className="px-4 py-2 font-medium">{a.clienteNome}</td>
              <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{a.servico}</td>
              <td className="px-4 py-2 text-center">
                <span className={cn('text-xs px-2 py-0.5 rounded-full font-medium', STATUS_COR[a.status] ?? 'bg-muted text-muted-foreground')}>
                  {a.status}
                </span>
              </td>
            </tr>
          ))}
          {!dados?.length && (
            <tr><td colSpan={4} className="px-4 py-6 text-center text-muted-foreground">Nenhum agendamento para hoje</td></tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
