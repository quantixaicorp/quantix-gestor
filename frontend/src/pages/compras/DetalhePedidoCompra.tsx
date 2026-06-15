import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import type { PedidoCompraResponse } from '@/types/compras'

const STATUS_COLORS: Record<string, string> = {
  Rascunho: 'bg-muted text-muted-foreground',
  AguardandoAprovacao: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400',
  Aprovado: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
  RecebidoParcialmente: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400',
  RecebidoTotalmente: 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400',
  Cancelado: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400',
}

export default function DetalhePedidoCompra() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { get, converter, cancelar } = usePedidosCompra()
  const [pedido, setPedido] = useState<PedidoCompraResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    get(id)
      .then(setPedido)
      .catch(() => toast.error('Erro ao carregar pedido'))
      .finally(() => setLoading(false))
  }, [id, get])

  async function handleConverter() {
    if (!pedido) return
    try {
      const compra = await converter(pedido.id)
      toast.success('Convertido em compra!')
      navigate(`/compras/${compra.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  async function handleCancelar() {
    if (!pedido || !confirm('Cancelar este pedido?')) return
    try {
      const updated = await cancelar(pedido.id)
      setPedido(updated)
      toast.success('Pedido cancelado.')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (!pedido) return <p className="text-muted-foreground">Pedido não encontrado.</p>

  const total = pedido.itens.reduce((acc, i) => acc + i.quantidade * i.valorEstimado, 0)

  return (
    <div className="space-y-4 max-w-3xl">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/compras/pedidos')}>
          <ArrowLeft size={18} />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold">Pedido #{pedido.numero}</h1>
            <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_COLORS[pedido.status] ?? ''}`}>
              {pedido.status}
            </span>
          </div>
          <p className="text-sm text-muted-foreground">{new Date(pedido.data).toLocaleDateString('pt-BR')} — {pedido.fornecedorNome}</p>
        </div>
        <div className="flex gap-2">
          {pedido.status !== 'Cancelado' && pedido.status !== 'RecebidoTotalmente' && (
            <>
              <Button onClick={() => void handleConverter()}>Converter em Compra</Button>
              <Button variant="outline" onClick={() => void handleCancelar()}>Cancelar</Button>
            </>
          )}
        </div>
      </div>

      {pedido.observacoes && (
        <p className="text-sm text-muted-foreground rounded-xl border bg-card p-4">{pedido.observacoes}</p>
      )}

      <div className="rounded-xl border bg-card p-6">
        <h2 className="font-semibold mb-4">Itens do Pedido</h2>
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-3 py-2 text-left font-medium">Descrição</th>
                <th className="px-3 py-2 text-right font-medium">Qtd</th>
                <th className="px-3 py-2 text-right font-medium">Valor Est.</th>
                <th className="px-3 py-2 text-right font-medium">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              {pedido.itens.map(i => (
                <tr key={i.id} className="border-b last:border-0">
                  <td className="px-3 py-2">{i.descricao}</td>
                  <td className="px-3 py-2 text-right">{i.quantidade}</td>
                  <td className="px-3 py-2 text-right">
                    {i.valorEstimado.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-3 py-2 text-right font-medium">
                    {(i.quantidade * i.valorEstimado).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t bg-muted/30">
                <td colSpan={3} className="px-3 py-2 text-right font-semibold">Total Estimado:</td>
                <td className="px-3 py-2 text-right font-bold">
                  {total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>
    </div>
  )
}
