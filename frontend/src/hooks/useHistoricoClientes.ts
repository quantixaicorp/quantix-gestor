import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  HistoricoClientesResponse,
  HistoricoClienteDetalheResponse,
} from '@/types/relatorios'

export function useHistoricoClientes() {
  const [lista, setLista] = useState<HistoricoClientesResponse | null>(null)
  const [loadingLista, setLoadingLista] = useState(false)
  const [detalhe, setDetalhe] = useState<HistoricoClienteDetalheResponse | null>(null)
  const [loadingDetalhe, setLoadingDetalhe] = useState(false)

  const loadLista = useCallback(async () => {
    setLoadingLista(true)
    try {
      const result = await api.get<HistoricoClientesResponse>('/api/relatorios/clientes/historico')
      setLista(result)
    } finally {
      setLoadingLista(false)
    }
  }, [])

  const loadDetalhe = useCallback(async (id: string) => {
    setDetalhe(null)
    setLoadingDetalhe(true)
    try {
      const result = await api.get<HistoricoClienteDetalheResponse>(
        `/api/relatorios/clientes/${id}/historico`
      )
      setDetalhe(result)
    } finally {
      setLoadingDetalhe(false)
    }
  }, [])

  const clearDetalhe = useCallback(() => setDetalhe(null), [])

  return { lista, loadingLista, loadLista, detalhe, loadingDetalhe, loadDetalhe, clearDetalhe }
}
