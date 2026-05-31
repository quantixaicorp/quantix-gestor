import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import { toast } from '@/hooks/useToast'
import type {
  ContratoListItem, ContratoResponse,
  CreateContratoRequest, GerarCobrancasRequest,
} from '@/types/contrato'
import type { CobrancaListItem } from '@/types/cobranca'

export function useContratos() {
  const [contratos, setContratos] = useState<ContratoListItem[]>([])
  const [contrato, setContrato] = useState<ContratoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (status?: string) => {
    setLoading(true)
    setError(null)
    try {
      const params = status ? `?status=${status}` : ''
      const items = await api.get<ContratoListItem[]>(`/api/contratos${params}`)
      setContratos(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar contratos')
    } finally { setLoading(false) }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.get<ContratoResponse>(`/api/contratos/${id}`)
      setContrato(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar contrato')
      return null
    } finally { setLoading(false) }
  }, [])

  const create = useCallback(async (req: CreateContratoRequest) => {
    return api.post<ContratoResponse>('/api/contratos', req)
  }, [])

  const ativar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/ativar`, {})
    setContrato(result)
    return result
  }, [])

  const encerrar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/encerrar`, {})
    setContrato(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/cancelar`, {})
    setContrato(result)
    return result
  }, [])

  const gerarCobrancas = useCallback(async (id: string, req: GerarCobrancasRequest) => {
    return api.post<CobrancaListItem[]>(`/api/contratos/${id}/gerar-cobrancas`, req)
  }, [])

  const downloadPdf = useCallback((id: string) => {
    const win = window.open(`${import.meta.env.VITE_API_URL}/api/contratos/${id}/pdf`, '_blank')
    if (!win) toast('Permite popups para abrir o PDF.')
  }, [])

  return { contratos, contrato, loading, error, list, get, create, ativar, encerrar, cancelar, gerarCobrancas, downloadPdf }
}
