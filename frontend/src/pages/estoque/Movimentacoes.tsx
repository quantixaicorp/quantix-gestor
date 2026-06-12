import { useEffect } from 'react'
import { useEstoque } from '@/hooks/useEstoque'
import { Badge } from '@/components/ui/badge'
import { KpiRow } from '@/components/ui/KpiRow'

export default function Movimentacoes() {
  const { movimentacoes, loading, listMovimentacoes } = useEstoque()

  useEffect(() => { listMovimentacoes() }, [listMovimentacoes])

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Movimentações de Estoque</h1>

      <KpiRow items={[
        { label: 'Total', value: String(movimentacoes.length) },
        { label: 'Entradas', value: String(movimentacoes.filter(m => m.tipo === 'Entrada').length), color: 'text-green-600 dark:text-green-400' },
        { label: 'Saídas', value: String(movimentacoes.filter(m => m.tipo !== 'Entrada').length), color: 'text-red-600 dark:text-red-400' },
        { label: 'Produtos movimentados', value: String(new Set(movimentacoes.map(m => m.produtoId)).size) },
      ]} />

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : movimentacoes.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhuma movimentação encontrada</p>
      ) : (
        <>
          {/* Mobile: card list */}
          <div className="md:hidden space-y-2">
            {movimentacoes.map(m => (
              <div key={m.id} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium truncate flex-1">{m.produtoNome}</p>
                  <Badge variant={m.tipo === 'Entrada' ? 'secondary' : 'destructive'}>
                    {m.tipo}
                  </Badge>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">{m.origem}</span>
                  <span className="font-semibold">Qtd: {m.quantidade}</span>
                </div>
                <div className="text-xs text-muted-foreground">
                  {new Date(m.dataHora).toLocaleString('pt-BR')}
                  {m.observacao && <span> · {m.observacao}</span>}
                </div>
              </div>
            ))}
          </div>

          {/* Desktop: table */}
          <div className="hidden md:block overflow-x-auto rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Produto</th>
                  <th className="px-4 py-3 text-left font-medium">Tipo</th>
                  <th className="px-4 py-3 text-right font-medium">Quantidade</th>
                  <th className="px-4 py-3 text-left font-medium">Origem</th>
                  <th className="px-4 py-3 text-left font-medium">Data</th>
                  <th className="px-4 py-3 text-left font-medium">Observação</th>
                </tr>
              </thead>
              <tbody>
                {movimentacoes.map(m => (
                  <tr key={m.id} className="border-b">
                    <td className="px-4 py-3 font-medium">{m.produtoNome}</td>
                    <td className="px-4 py-3">
                      <Badge variant={m.tipo === 'Entrada' ? 'secondary' : 'destructive'}>
                        {m.tipo}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-right">{m.quantidade}</td>
                    <td className="px-4 py-3 text-muted-foreground">{m.origem}</td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {new Date(m.dataHora).toLocaleString('pt-BR')}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">{m.observacao ?? '—'}</td>
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
