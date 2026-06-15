import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  CompraResponse, CompraResumoResponse,
  CreateCompraRequest, UpdateCompraRequest,
} from '@/types/compras'

export function useCompras() {
  const [compras, setCompras] = useState<CompraResponse[]>([])
  const [resumo, setResumo] = useState<CompraResumoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: {
    status?: string; fornecedorId?: string; de?: string; ate?: string
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams(
        Object.entries(params ?? {}).filter(([, v]) => v) as [string, string][]
      ).toString()
      const data = await api.get<CompraResponse[]>(`/api/compras${qs ? `?${qs}` : ''}`)
      setCompras(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar compras')
    } finally {
      setLoading(false)
    }
  }, [])

  const getResumo = useCallback(async () => {
    const data = await api.get<CompraResumoResponse>('/api/compras/resumo')
    setResumo(data)
  }, [])

  const get = useCallback(
    (id: string) => api.get<CompraResponse>(`/api/compras/${id}`),
    []
  )

  const create = useCallback(async (req: CreateCompraRequest) => {
    const result = await api.post<CompraResponse>('/api/compras', req)
    setCompras(prev => [result, ...prev])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateCompraRequest) => {
    const result = await api.put<CompraResponse>(`/api/compras/${id}`, req)
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const confirmar = useCallback(async (id: string) => {
    const result = await api.post<CompraResponse>(`/api/compras/${id}/confirmar`, {})
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<CompraResponse>(`/api/compras/${id}/cancelar`, {})
    setCompras(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/compras/${id}`)
    setCompras(prev => prev.filter(c => c.id !== id))
  }, [])

  return { compras, resumo, loading, error, list, getResumo, get, create, update, confirmar, cancelar, remove }
}
