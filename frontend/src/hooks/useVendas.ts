import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { VendaListItem, VendaResponse, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'

export function useVendas() {
  const [vendas, setVendas] = useState<VendaListItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: {
    de?: string; ate?: string; status?: string
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (params?.de) qs.set('de', params.de)
      if (params?.ate) qs.set('ate', params.ate)
      if (params?.status) qs.set('status', params.status)
      const data = await api.get<VendaListItem[]>(`/api/vendas?${qs}`)
      setVendas(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar vendas')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(async (id: string) =>
    api.get<VendaResponse>(`/api/vendas/${id}`), [])

  const create = useCallback(async (req: CreateVendaRequest) => {
    const result = await api.post<VendaResponse>('/api/vendas', req)
    setVendas(prev => [
      {
        id: result.id, clienteNome: result.clienteNome,
        dataHora: result.dataHora, status: result.status,
        total: result.total, formaPagamento: result.formaPagamento,
      },
      ...prev,
    ])
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<VendaResponse>(`/api/vendas/${id}/cancelar`, {})
    setVendas(prev => prev.map(v => v.id === id ? { ...v, status: result.status } : v))
    return result
  }, [])

  const fechar = useCallback(async (id: string, req: FecharVendaRequest) => {
    const result = await api.post<VendaResponse>(`/api/vendas/${id}/fechar`, req)
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/vendas/${id}`)
    setVendas(prev => prev.filter(v => v.id !== id))
  }, [])

  return { vendas, loading, error, list, get, create, cancelar, fechar, remove }
}
