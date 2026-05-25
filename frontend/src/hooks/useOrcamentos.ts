import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { OrcamentoListItem, OrcamentoResponse, CreateOrcamentoRequest } from '@/types/orcamento'

export function useOrcamentos() {
  const [orcamentos, setOrcamentos] = useState<OrcamentoListItem[]>([])
  const [orcamento, setOrcamento] = useState<OrcamentoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (status?: string) => {
    setLoading(true)
    try {
      const qs = status ? `?status=${status}` : ''
      const data = await api.get<OrcamentoListItem[]>(`/api/orcamentos${qs}`)
      setOrcamentos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar orçamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    try {
      const data = await api.get<OrcamentoResponse>(`/api/orcamentos/${id}`)
      setOrcamento(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar orçamento')
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateOrcamentoRequest) => {
    const result = await api.post<OrcamentoResponse>('/api/orcamentos', req)
    return result
  }, [])

  const enviar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/enviar`, {})
    setOrcamento(result)
    return result
  }, [])

  const aprovar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/aprovar`, {})
    setOrcamento(result)
    return result
  }, [])

  const rejeitar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/rejeitar`, {})
    setOrcamento(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/cancelar`, {})
    setOrcamento(result)
    return result
  }, [])

  const converter = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/converter`, {})
    setOrcamento(result)
    return result
  }, [])

  return {
    orcamentos, orcamento, loading, error,
    list, get, create, enviar, aprovar, rejeitar, cancelar, converter,
  }
}
