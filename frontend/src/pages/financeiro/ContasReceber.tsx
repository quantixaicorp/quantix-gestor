import { useEffect } from 'react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Badge } from '@/components/ui/badge'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasReceber() {
  const { lancamentos, loading, list } = useFinanceiro()

  useEffect(() => { void list({ tipo: 'Receita', status: 'Pendente' }) }, [list])

  const total = lancamentos.reduce((s, l) => s + l.valor, 0)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Contas a Receber</h1>
        <div className="text-right">
          <p className="text-sm text-muted-foreground">Total a receber</p>
          <p className="text-xl font-bold text-primary">{fmt(total)}</p>
        </div>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : lancamentos.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma conta a receber</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {lancamentos.map(l => (
              <div key={l.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium truncate flex-1">{l.descricao}</p>
                  <Badge variant={l.vencido ? 'destructive' : 'outline'} className="shrink-0">
                    {l.vencido ? 'Vencida' : 'Pendente'}
                  </Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{l.categoria}</span>
                  <span className="font-semibold text-primary">{fmt(l.valor)}</span>
                </div>
                <p className="text-xs text-muted-foreground">Vence: {fmtDate(l.dataVencimento)}</p>
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
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {lancamentos.map(l => (
                  <tr key={l.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{l.descricao}</td>
                    <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                    <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                    <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                    <td className="px-4 py-3">
                      <Badge variant={l.vencido ? 'destructive' : 'outline'}>
                        {l.vencido ? 'Vencida' : 'Pendente'}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  )
}
