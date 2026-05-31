import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  NotaFiscalResponse,
  EmitirNotaFiscalRequest,
  CancelarNotaFiscalRequest,
} from '@/types/fiscal'

export function useNotasFiscais() {
  const [notas, setNotas] = useState<NotaFiscalResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<NotaFiscalResponse[]>('/api/notas-fiscais')
      setNotas(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar notas fiscais')
    } finally {
      setLoading(false)
    }
  }, [])

  const emitir = useCallback(async (req: EmitirNotaFiscalRequest) => {
    const result = await api.post<NotaFiscalResponse>('/api/notas-fiscais/emitir', req)
    setNotas(prev => [result, ...prev])
    return result
  }, [])

  const cancelar = useCallback(async (id: string, req: CancelarNotaFiscalRequest) => {
    const result = await api.post<NotaFiscalResponse>(`/api/notas-fiscais/${id}/cancelar`, req)
    setNotas(prev => prev.map(n => n.id === id ? result : n))
    return result
  }, [])

  const consultar = useCallback(async (id: string) => {
    const result = await api.post<NotaFiscalResponse>(`/api/notas-fiscais/${id}/consultar`, {})
    setNotas(prev => prev.map(n => n.id === id ? result : n))
    return result
  }, [])

  return { notas, loading, error, list, emitir, cancelar, consultar }
}
