import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  AgingData, CobrancaAsaasResponse, CobrancaListItem, CobrancaResponse,
  CreateCobrancaRequest, PagarCobrancaRequest,
} from '@/types/cobranca'

interface CobrancaResumo {
  totalAReceber: number
  totalVencido: number
  totalRecebidoNoMes: number
}

export function useCobrancas() {
  const [cobrancas, setCobrancas] = useState<CobrancaListItem[]>([])
  const [cobranca, setCobranca] = useState<CobrancaResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: { status?: string; clienteId?: string; mes?: string }) => {
    setLoading(true)
    setError(null)
    try {
      const q = new URLSearchParams()
      if (params?.status) q.set('status', params.status)
      if (params?.clienteId) q.set('clienteId', params.clienteId)
      if (params?.mes) q.set('mes', params.mes)
      const items = await api.get<CobrancaListItem[]>(`/api/cobrancas?${q}`)
      setCobrancas(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar cobranças')
    } finally { setLoading(false) }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.get<CobrancaResponse>(`/api/cobrancas/${id}`)
      setCobranca(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar cobrança')
      return null
    } finally { setLoading(false) }
  }, [])

  const create = useCallback(async (req: CreateCobrancaRequest) => {
    return api.post<CobrancaResponse>('/api/cobrancas', req)
  }, [])

  const pagar = useCallback(async (id: string, req: PagarCobrancaRequest) => {
    const result = await api.post<CobrancaResponse>(`/api/cobrancas/${id}/pagar`, req)
    setCobranca(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<CobrancaResponse>(`/api/cobrancas/${id}/cancelar`, {})
    setCobranca(result)
    return result
  }, [])

  const abrirWhatsapp = useCallback(async (id: string) => {
    const { url } = await api.get<{ url: string }>(`/api/cobrancas/${id}/whatsapp`)
    window.open(url, '_blank')
  }, [])

  const fetchAging = useCallback(async (): Promise<AgingData> => {
    return api.get<AgingData>('/api/cobrancas/aging')
  }, [])

  const fetchResumo = useCallback(async (): Promise<CobrancaResumo> => {
    return api.get<CobrancaResumo>('/api/cobrancas/resumo')
  }, [])

  const enviarAsaas = useCallback(async (id: string, billingType: 'PIX' | 'BOLETO') => {
    return api.post<CobrancaAsaasResponse>(`/api/cobrancas/${id}/enviar-asaas`, { billingType })
  }, [])

  return { cobrancas, cobranca, loading, error, list, get, create, pagar, cancelar, abrirWhatsapp, fetchAging, fetchResumo, enviarAsaas }
}
