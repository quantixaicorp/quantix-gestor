import { useEffect } from 'react'
import { useEstoque } from '@/hooks/useEstoque'
import { Badge } from '@/components/ui/badge'

export default function Movimentacoes() {
  const { movimentacoes, loading, listMovimentacoes } = useEstoque()

  useEffect(() => { listMovimentacoes() }, [listMovimentacoes])

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Movimentações de Estoque</h1>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
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
      )}
    </div>
  )
}
