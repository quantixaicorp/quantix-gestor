import type { UltimaVendaItem } from '@/types/dashboard'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: UltimaVendaItem[] }

export default function TabelaUltimasVendas({ dados }: Props) {
  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <div className="px-4 py-3 border-b bg-muted/30">
        <p className="text-sm font-semibold">Últimas Vendas</p>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-muted/40">
          <tr>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Data/Hora</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground">Cliente</th>
            <th className="text-left px-4 py-2 font-medium text-muted-foreground hidden sm:table-cell">Pagamento</th>
            <th className="text-right px-4 py-2 font-medium text-muted-foreground">Total</th>
          </tr>
        </thead>
        <tbody>
          {(dados ?? []).map((v, i) => (
            <tr key={i} className="border-t hover:bg-muted/20">
              <td className="px-4 py-2 text-muted-foreground whitespace-nowrap">
                {new Date(v.dataHora).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
              </td>
              <td className="px-4 py-2">{v.clienteNome}</td>
              <td className="px-4 py-2 hidden sm:table-cell text-muted-foreground">{v.formaPagamento}</td>
              <td className="px-4 py-2 text-right font-medium">{fmt(v.total)}</td>
            </tr>
          ))}
          {!dados?.length && (
            <tr><td colSpan={4} className="px-4 py-6 text-center text-muted-foreground">Sem vendas recentes</td></tr>
          )}
        </tbody>
      </table>
    </div>
  )
}
