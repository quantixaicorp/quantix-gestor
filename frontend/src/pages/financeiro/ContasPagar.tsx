import { useEffect, useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import type { LancamentoResponse } from '@/types/financeiro'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasPagar() {
  const { lancamentos, loading, list, pagar } = useFinanceiro()
  const [pagando, setPagando] = useState<string | null>(null)

  useEffect(() => { void list({ tipo: 'Despesa', status: 'Pendente' }) }, [list])

  const vencidas = lancamentos.filter(l => l.vencido)
  const proximas = lancamentos.filter(l => !l.vencido)
  const totalPendente = lancamentos.reduce((s, l) => s + l.valor, 0)

  async function handlePagar(l: LancamentoResponse) {
    if (!confirm(`Confirmar pagamento de ${fmt(l.valor)} — ${l.descricao}?`)) return
    setPagando(l.id)
    try { await pagar(l.id, { dataPagamento: new Date().toISOString() }) }
    finally { setPagando(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
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

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
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
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma conta a pagar
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
