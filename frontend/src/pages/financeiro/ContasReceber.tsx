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
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Contas a Receber</h1>
        <div className="text-right">
          <p className="text-sm text-muted-foreground">Total a receber</p>
          <p className="text-xl font-bold text-primary">{fmt(total)}</p>
        </div>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
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
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma conta a receber
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
