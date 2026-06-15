import type { CobrancaVencidaItem } from '@/types/dashboard'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: CobrancaVencidaItem[] }

export default function TabelaCobrancasVencidas({ dados }: Props) {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <div className="px-4 py-3 border-b bg-muted/30 flex items-center justify-between">
        <p className="text-sm font-semibold">Cobranças Vencidas</p>
        {dados?.length > 0 && (
          <span className="text-xs bg-red-100 text-red-700 dark:bg-red-950/30 dark:text-red-400 px-2 py-0.5 rounded-full font-medium">
            {dados.length}
          </span>
        )}
      </div>
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Referência</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Cliente</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Valor</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Atraso</th>
          </tr>
        </thead>
        <tbody>
          {(dados ?? []).map((c, i) => (
            <tr key={i} className="border-t hover:bg-muted/20">
              <td className="px-4 py-2 font-medium">{c.referencia}</td>
              <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{c.clienteNome}</td>
              <td className="px-4 py-2 text-right font-medium text-red-600">{fmt(c.valor)}</td>
              <td className="px-4 py-2 text-right">
                <span className={cn('text-xs px-1.5 py-0.5 rounded font-medium',
                  c.diasAtraso <= 7 ? 'bg-yellow-100 text-yellow-700' :
                  c.diasAtraso <= 30 ? 'bg-orange-100 text-orange-700' : 'bg-red-100 text-red-700'
                )}>
                  {c.diasAtraso}d
                </span>
              </td>
            </tr>
          ))}
          {!dados?.length && (
            <tr><td colSpan={4} className="px-4 py-6 text-center text-muted-foreground">Nenhuma cobrança vencida</td></tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
