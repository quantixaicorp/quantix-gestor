import { useEffect, useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { useConfirm } from '@/hooks/useConfirm'
import type { LancamentoResponse } from '@/types/financeiro'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasPagar() {
  const { lancamentos, loading, list, pagar } = useFinanceiro()
  const [pagando, setPagando] = useState<string | null>(null)
  const { confirm, ConfirmDialogNode } = useConfirm()

  useEffect(() => { void list({ tipo: 'Despesa', status: 'Pendente' }) }, [list])

  const vencidas = lancamentos.filter(l => l.vencido)
  const proximas = lancamentos.filter(l => !l.vencido)
  const totalPendente = lancamentos.reduce((s, l) => s + l.valor, 0)

  async function handlePagar(l: LancamentoResponse) {
    const ok = await confirm({
      title: 'Confirmar pagamento?',
      description: `${fmt(l.valor)} — ${l.descricao}`,
    })
    if (!ok) return
    setPagando(l.id)
    try { await pagar(l.id, { dataPagamento: new Date().toISOString() }) }
    finally { setPagando(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Contas a Pagar</h1>
        <div className="text-right">
          <p className="text-sm text-muted-foreground">Total pendente</p>
          <p className="text-xl font-bold text-destructive">{fmt(totalPendente)}</p>
        </div>
      </div>

      {vencidas.length > 0 && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-3 flex gap-2">
          <AlertTriangle size={18} className="text-destructive mt-0.5 shrink-0" />
          <p className="text-sm text-destructive">
            <strong>{vencidas.length} conta(s) vencida(s)</strong> totalizando {fmt(vencidas.reduce((s, l) => s + l.valor, 0))}.
            Regularize o quanto antes.
          </p>
        </div>
      )}

      {loading ? <p className="text-muted-foreground">Carregando...</p> : lancamentos.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma conta a pagar</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {[...vencidas, ...proximas].map(l => (
              <div key={l.id} className={`rounded-lg border bg-card p-4 space-y-2 ${l.vencido ? 'border-destructive/40 bg-destructive/5' : ''}`}>
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium truncate flex-1">{l.descricao}</p>
                  {l.vencido && <Badge variant="destructive" className="text-xs shrink-0">Vencida</Badge>}
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{l.categoria}</span>
                  <span className="font-semibold">{fmt(l.valor)}</span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</span>
                  <Button size="sm" variant="outline" disabled={pagando === l.id} onClick={() => handlePagar(l)}>
                    {pagando === l.id ? '...' : 'Pagar'}
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Descrição</th>
                  <th className="px-4 py-3 text-left font-medium">Categoria</th>
                  <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                  <th className="px-4 py-3 text-right font-medium">Valor</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody>
                {[...vencidas, ...proximas].map(l => (
                  <tr key={l.id} className={`border-b ${l.vencido ? 'bg-destructive/5' : ''}`}>
                    <td className="px-4 py-3 font-medium">
                      {l.descricao}
                      {l.vencido && <Badge variant="destructive" className="ml-2 text-xs">Vencida</Badge>}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                    <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                    <td className="px-4 py-3">
                      <Button size="sm" variant="outline"
                        disabled={pagando === l.id}
                        onClick={() => handlePagar(l)}>
                        {pagando === l.id ? '...' : 'Pagar'}
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
      {ConfirmDialogNode}
    </div>
  )
}
