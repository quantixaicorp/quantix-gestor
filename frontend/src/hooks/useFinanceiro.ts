import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  LancamentoResponse, CreateLancamentoRequest,
  PagarLancamentoRequest, FluxoCaixaResponse,
} from '@/types/financeiro'

export function useFinanceiro() {
  const [lancamentos, setLancamentos] = useState<LancamentoResponse[]>([])
  const [fluxo, setFluxo] = useState<FluxoCaixaResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: {
    tipo?: string; status?: string; vencimentoAte?: string
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (params?.tipo) qs.set('tipo', params.tipo)
      if (params?.status) qs.set('status', params.status)
      if (params?.vencimentoAte) qs.set('vencimentoAte', params.vencimentoAte)
      const data = await api.get<LancamentoResponse[]>(`/api/lancamentos?${qs}`)
      setLancamentos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar lançamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateLancamentoRequest) => {
    const result = await api.post<LancamentoResponse>('/api/lancamentos', req)
    setLancamentos(prev => [...prev, result])
    return result
  }, [])

  const pagar = useCallback(async (id: string, req: PagarLancamentoRequest) => {
    const result = await api.post<LancamentoResponse>(`/api/lancamentos/${id}/pagar`, req)
    setLancamentos(prev => prev.map(l => l.id === id ? result : l))
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<LancamentoResponse>(`/api/lancamentos/${id}/cancelar`, {})
    setLancamentos(prev => prev.map(l => l.id === id ? result : l))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/lancamentos/${id}`)
    setLancamentos(prev => prev.filter(l => l.id !== id))
  }, [])

  const getFluxoCaixa = useCallback(async (de: string, ate: string) => {
    const data = await api.get<FluxoCaixaResponse>(
      `/api/financeiro/fluxo-caixa?de=${de}&ate=${ate}`)
    setFluxo(data)
    return data
  }, [])

  return { lancamentos, fluxo, loading, error, list, create, pagar, cancelar, remove, getFluxoCaixa }
}
