import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  PedidoCompraResponse, CreatePedidoCompraRequest,
  UpdatePedidoCompraRequest, CompraResponse,
} from '@/types/compras'

export function usePedidosCompra() {
  const [pedidos, setPedidos] = useState<PedidoCompraResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: { status?: string; fornecedorId?: string }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams(
        Object.entries(params ?? {}).filter(([, v]) => v) as [string, string][]
      ).toString()
      const data = await api.get<PedidoCompraResponse[]>(`/api/pedidos-compra${qs ? `?${qs}` : ''}`)
      setPedidos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar pedidos')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(
    (id: string) => api.get<PedidoCompraResponse>(`/api/pedidos-compra/${id}`),
    []
  )

  const create = useCallback(async (req: CreatePedidoCompraRequest) => {
    const result = await api.post<PedidoCompraResponse>('/api/pedidos-compra', req)
    setPedidos(prev => [result, ...prev])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdatePedidoCompraRequest) => {
    const result = await api.put<PedidoCompraResponse>(`/api/pedidos-compra/${id}`, req)
    setPedidos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const converter = useCallback(
    (id: string) => api.post<CompraResponse>(`/api/pedidos-compra/${id}/converter`, {}),
    []
  )

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<PedidoCompraResponse>(`/api/pedidos-compra/${id}/cancelar`, {})
    setPedidos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  return { pedidos, loading, error, list, get, create, update, converter, cancelar }
}
