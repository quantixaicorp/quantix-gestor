import type { ContratoVencendoItem } from '@/types/dashboard'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: ContratoVencendoItem[] }

export default function TabelaContratosVencendo({ dados }: Props) {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <div className="px-4 py-3 border-b bg-muted/30">
        <p className="text-sm font-semibold">Contratos Vencendo nos Próximos 30 Dias</p>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Contrato</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Cliente</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Valor</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Dias</th>
          </tr>
        </thead>
        <tbody>
          {(dados ?? []).map((c, i) => (
            <tr key={i} className="border-t hover:bg-muted/20">
              <td className="px-4 py-2 font-medium">{c.titulo}</td>
              <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{c.clienteNome}</td>
              <td className="px-4 py-2 text-right font-medium">{fmt(c.valor)}</td>
              <td className="px-4 py-2 text-right">
                <span className={cn('text-xs px-1.5 py-0.5 rounded font-medium',
                  c.diasRestantes <= 7 ? 'bg-red-100 text-red-700' :
                  c.diasRestantes <= 15 ? 'bg-orange-100 text-orange-700' : 'bg-yellow-100 text-yellow-700'
                )}>
                  {c.diasRestantes}d
                </span>
              </td>
            </tr>
          ))}
          {!dados?.length && (
            <tr><td colSpan={4} className="px-4 py-6 text-center text-muted-foreground">Nenhum contrato vencendo em 30 dias</td></tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
