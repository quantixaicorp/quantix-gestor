import { Users, ShoppingCart, DollarSign } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import type { RelatorioClientesResponse } from '@/types/relatorios'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

interface Props { dados: RelatorioClientesResponse }

export default function AbaClientes({ dados }: Props) {
  const taxaAtividade = dados.totalClientes > 0
    ? (dados.clientesCompraram / dados.totalClientes * 100).toFixed(1)
    : '0.0'

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <KpiCard titulo="Total de clientes" valor={dados.totalClientes.toLocaleString('pt-BR')} icon={Users} />
        <KpiCard titulo="Compraram no período" valor={dados.clientesCompraram.toLocaleString('pt-BR')} icon={ShoppingCart} cor="green" />
        <KpiCard titulo="Ticket médio / cliente" valor={fmt(dados.ticketMedioCliente)} icon={DollarSign} />
        <KpiCard titulo="Taxa de atividade" valor={`${taxaAtividade}%`} icon={Users}
          cor={Number(taxaAtividade) >= 30 ? 'green' : 'yellow'} />
      </div>

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Ranking de clientes por faturamento</p>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground w-8">#</th>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Cliente</th>
              <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden sm:table-cell">WhatsApp</th>
              <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Compras</th>
              <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Total</th>
            </tr>
          </thead>
          <tbody>
            {dados.topClientes.map((c, idx) => (
              <tr key={idx} className="border-t hover:bg-muted/30 transition-colors">
                <td className="px-4 py-2.5 text-muted-foreground font-medium">{idx + 1}</td>
                <td className="px-4 py-2.5 font-medium">{c.nome}</td>
                <td className="px-4 py-2.5 text-muted-foreground hidden sm:table-cell">
                  {c.whatsapp || <span className="text-muted-foreground/50">—</span>}
                </td>
                <td className="px-4 py-2.5 text-right">{c.compras}</td>
                <td className="px-4 py-2.5 text-right font-semibold">{fmt(c.totalGasto)}</td>
              </tr>
            ))}
            {dados.topClientes.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                  Nenhuma venda com cliente identificado no período
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
