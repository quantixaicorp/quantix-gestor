import { useEffect, useState } from 'react'
import { Users, Repeat, UserX, Sparkles, AlertTriangle, DollarSign, Clock, Loader2 } from 'lucide-react'
import KpiCard from '@/components/dashboard/KpiCard'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { useHistoricoClientes } from '@/hooks/useHistoricoClientes'
import { cn } from '@/lib/utils'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtData = (s: string) => new Date(s).toLocaleDateString('pt-BR')
const fmtDias = (d: number | null) => (d == null ? '—' : `${d} dia${d === 1 ? '' : 's'}`)

const badgeClasses: Record<string, string> = {
  Recorrente: 'bg-green-100 text-green-700 dark:bg-green-950 dark:text-green-400',
  Inativo: 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-400',
  Novo: 'bg-blue-100 text-blue-700 dark:bg-blue-950 dark:text-blue-400',
  'Sem compras': 'bg-muted text-muted-foreground',
}

function Badge({ classificacao, emRisco }: { classificacao: string; emRisco?: boolean }) {
  return (
    <span className="inline-flex items-center gap-1">
      <span className={cn('rounded-full px-2 py-0.5 text-xs font-medium',
        badgeClasses[classificacao] ?? 'bg-muted text-muted-foreground')}>
        {classificacao}
      </span>
      {emRisco && (
        <span className="rounded-full bg-yellow-100 text-yellow-700 dark:bg-yellow-950 dark:text-yellow-400 px-2 py-0.5 text-xs font-medium">
          Em risco
        </span>
      )}
    </span>
  )
}

export default function AbaHistoricoClientes() {
  const { lista, loadingLista, loadLista, detalhe, loadingDetalhe, loadDetalhe, clearDetalhe } =
    useHistoricoClientes()
  const [aberto, setAberto] = useState(false)

  useEffect(() => { void loadLista() }, [loadLista])

  function abrirDetalhe(id: string) {
    setAberto(true)
    void loadDetalhe(id)
  }

  function fecharDetalhe(open: boolean) {
    setAberto(open)
    if (!open) clearDetalhe()
  }

  if (loadingLista) {
    return (
      <div className="flex h-48 items-center justify-center gap-2 text-muted-foreground">
        <Loader2 size={18} className="animate-spin" />
        <span>Carregando...</span>
      </div>
    )
  }

  if (!lista) return null

  const pct = (n: number) =>
    lista.totalClientesComCompras > 0
      ? `${(n / lista.totalClientesComCompras * 100).toFixed(0)}%`
      : '0%'

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <KpiCard titulo="Clientes com compras" valor={lista.totalClientesComCompras.toLocaleString('pt-BR')} icon={Users} />
        <KpiCard titulo="Recorrentes" valor={lista.recorrentes.toLocaleString('pt-BR')} detalhe={`${pct(lista.recorrentes)} da base`} icon={Repeat} cor="green" />
        <KpiCard titulo="Inativos" valor={lista.inativos.toLocaleString('pt-BR')} detalhe={`${pct(lista.inativos)} da base`} icon={UserX} cor="red" />
        <KpiCard titulo="Novos" valor={lista.novos.toLocaleString('pt-BR')} detalhe={`${pct(lista.novos)} da base`} icon={Sparkles} cor="default" />
        <KpiCard titulo="Em risco" valor={lista.emRisco.toLocaleString('pt-BR')} detalhe="Recorrentes 61–90 dias sem comprar" icon={AlertTriangle} cor="yellow" />
        <KpiCard titulo="LTV médio" valor={fmt(lista.ltvMedio)} detalhe="Total gasto médio por cliente" icon={DollarSign} />
        <KpiCard titulo="Ticket médio geral" valor={fmt(lista.ticketMedioGeral)} icon={DollarSign} />
        <KpiCard titulo="Tempo médio entre compras" valor={fmtDias(lista.tempoMedioEntreComprasGeralDias)} icon={Clock} />
      </div>

      <div className="rounded-lg border bg-card overflow-hidden">
        <div className="px-4 py-3 border-b bg-muted/30">
          <p className="text-sm font-semibold">Histórico por cliente</p>
          <p className="text-xs text-muted-foreground">Clique em um cliente para ver o histórico completo de compras</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Cliente</th>
                <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Pedidos</th>
                <th className="text-right px-4 py-2.5 font-medium text-muted-foreground">Total gasto</th>
                <th className="text-right px-4 py-2.5 font-medium text-muted-foreground hidden md:table-cell">Ticket médio</th>
                <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden lg:table-cell">1ª compra</th>
                <th className="text-left px-4 py-2.5 font-medium text-muted-foreground hidden sm:table-cell">Última compra</th>
                <th className="text-right px-4 py-2.5 font-medium text-muted-foreground hidden lg:table-cell">Intervalo médio</th>
                <th className="text-left px-4 py-2.5 font-medium text-muted-foreground">Status</th>
              </tr>
            </thead>
            <tbody>
              {lista.clientes.map(c => {
                const emRisco = c.classificacao === 'Recorrente' && c.diasDesdeUltimaCompra >= 61
                return (
                  <tr
                    key={c.clienteId}
                    onClick={() => abrirDetalhe(c.clienteId)}
                    className="border-t hover:bg-muted/30 transition-colors cursor-pointer"
                  >
                    <td className="px-4 py-2.5 font-medium">{c.nome}</td>
                    <td className="px-4 py-2.5 text-right">{c.qtdPedidos}</td>
                    <td className="px-4 py-2.5 text-right font-semibold">{fmt(c.totalGasto)}</td>
                    <td className="px-4 py-2.5 text-right hidden md:table-cell">{fmt(c.ticketMedio)}</td>
                    <td className="px-4 py-2.5 text-muted-foreground hidden lg:table-cell">{fmtData(c.primeiraCompra)}</td>
                    <td className="px-4 py-2.5 text-muted-foreground hidden sm:table-cell">{fmtData(c.ultimaCompra)}</td>
                    <td className="px-4 py-2.5 text-right text-muted-foreground hidden lg:table-cell">{fmtDias(c.tempoMedioEntreComprasDias)}</td>
                    <td className="px-4 py-2.5"><Badge classificacao={c.classificacao} emRisco={emRisco} /></td>
                  </tr>
                )
              })}
              {lista.clientes.length === 0 && (
                <tr>
                  <td colSpan={8} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum cliente com compras concluídas
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Dialog open={aberto} onOpenChange={fecharDetalhe}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{detalhe?.nome ?? 'Histórico do cliente'}</DialogTitle>
          </DialogHeader>

          {loadingDetalhe || !detalhe ? (
            <div className="flex h-40 items-center justify-center gap-2 text-muted-foreground">
              <Loader2 size={18} className="animate-spin" />
              <span>Carregando...</span>
            </div>
          ) : (
            <div className="space-y-4">
              <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
                <Badge classificacao={detalhe.classificacao} />
                {detalhe.whatsapp && <span>· {detalhe.whatsapp}</span>}
                {detalhe.email && <span>· {detalhe.email}</span>}
                <span>· Cliente desde {fmtData(detalhe.dataCadastro)}</span>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Total gasto</p>
                  <p className="text-lg font-bold">{fmt(detalhe.totalGasto)}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Pedidos</p>
                  <p className="text-lg font-bold">{detalhe.qtdPedidos}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Ticket médio</p>
                  <p className="text-lg font-bold">{fmt(detalhe.ticketMedio)}</p>
                </div>
                <div className="rounded-lg border bg-card p-3">
                  <p className="text-xs text-muted-foreground">Intervalo médio</p>
                  <p className="text-lg font-bold">{fmtDias(detalhe.tempoMedioEntreComprasDias)}</p>
                </div>
              </div>

              <div className="rounded-lg border bg-card overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-muted/50">
                    <tr>
                      <th className="text-left px-3 py-2 font-medium text-muted-foreground">Data</th>
                      <th className="text-right px-3 py-2 font-medium text-muted-foreground">Itens</th>
                      <th className="text-left px-3 py-2 font-medium text-muted-foreground hidden sm:table-cell">Pagamento</th>
                      <th className="text-right px-3 py-2 font-medium text-muted-foreground">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detalhe.compras.map(co => (
                      <tr key={co.vendaId} className="border-t">
                        <td className="px-3 py-2">{fmtData(co.dataHora)}</td>
                        <td className="px-3 py-2 text-right">{co.qtdItens}</td>
                        <td className="px-3 py-2 text-muted-foreground hidden sm:table-cell">{co.formaPagamento}</td>
                        <td className="px-3 py-2 text-right font-semibold">{fmt(co.total)}</td>
                      </tr>
                    ))}
                    {detalhe.compras.length === 0 && (
                      <tr>
                        <td colSpan={4} className="px-3 py-6 text-center text-muted-foreground">
                          Sem compras concluídas
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  )
}
