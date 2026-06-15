import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Search } from 'lucide-react'
import { usePedidosCompra } from '@/hooks/usePedidosCompra'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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

export default function PedidosCompra() {
  const navigate = useNavigate()
  const { pedidos, loading, list, converter, cancelar } = usePedidosCompra()
  const [busca, setBusca] = useState('')

  useEffect(() => { void list() }, [list])

  async function handleConverter(p: PedidoCompraResponse) {
    try {
      const compra = await converter(p.id)
      toast.success('Convertido em compra!')
      navigate(`/compras/${compra.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao converter')
    }
  }

  async function handleCancelar(p: PedidoCompraResponse) {
    if (!confirm(`Cancelar pedido #${p.numero}?`)) return
    try {
      await cancelar(p.id)
      toast.success('Pedido cancelado.')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    }
  }

  const filtrados = pedidos.filter(p =>
    p.fornecedorNome.toLowerCase().includes(busca.toLowerCase()) ||
    String(p.numero).includes(busca)
  )

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">Pedidos de Compra</h1>
        <Button onClick={() => navigate('/compras/pedidos/novo')}>
          <Plus size={16} className="mr-2" /> Novo Pedido
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por fornecedor ou nº..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : filtrados.length === 0 ? (
        <p className="text-center text-muted-foreground py-12">Nenhum pedido de compra registrado</p>
      ) : (
        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nº</th>
                <th className="px-4 py-3 text-left font-medium">Data</th>
                <th className="px-4 py-3 text-left font-medium">Fornecedor</th>
                <th className="px-4 py-3 text-right font-medium">Valor Est.</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3 text-left font-medium w-44">Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map(p => (
                <tr
                  key={p.id}
                  className="border-b hover:bg-muted/30 cursor-pointer"
                  onClick={() => navigate(`/compras/pedidos/${p.id}`)}
                >
                  <td className="px-4 py-3 font-medium">#{p.numero}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(p.data).toLocaleDateString('pt-BR')}
                  </td>
                  <td className="px-4 py-3">{p.fornecedorNome}</td>
                  <td className="px-4 py-3 text-right">
                    {p.valorEstimado.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${STATUS_COLORS[p.status] ?? ''}`}>
                      {p.status}
                    </span>
                  </td>
                  <td className="px-4 py-3" onClick={e => e.stopPropagation()}>
                    <div className="flex gap-1">
                      {(p.status === 'Aprovado' || p.status === 'Rascunho') && (
                        <Button size="sm" variant="outline" onClick={() => void handleConverter(p)}>
                          Converter
                        </Button>
                      )}
                      {p.status !== 'Cancelado' && p.status !== 'RecebidoTotalmente' && (
                        <Button size="sm" variant="ghost" className="text-destructive" onClick={() => void handleCancelar(p)}>
                          Cancelar
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
