import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  AgendamentoListItem,
  AgendamentoResponse,
  CriarAgendamentoRequest,
  ConcluirResponse,
} from '@/types/agendamento'

export function useAgendamentos() {
  const [agendamentos, setAgendamentos] = useState<AgendamentoListItem[]>([])
  const [agendamento, setAgendamento] = useState<AgendamentoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (data: string) => {
    setLoading(true)
    try {
      const items = await api.get<AgendamentoListItem[]>(`/api/agendamentos?data=${data}`)
      setAgendamentos(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar agendamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const listSemana = useCallback(async (de: string, ate: string, profissionalId?: string) => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ de, ate })
      if (profissionalId) params.set('profissionalId', profissionalId)
      const items = await api.get<AgendamentoListItem[]>(`/api/agendamentos/semana?${params}`)
      setAgendamentos(items)
      return items
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar agenda')
      return []
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    try {
      const data = await api.get<AgendamentoResponse>(`/api/agendamentos/${id}`)
      setAgendamento(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar agendamento')
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CriarAgendamentoRequest) => {
    return api.post<AgendamentoResponse>('/api/agendamentos', req)
  }, [])

  const confirmar = useCallback(async (id: string) => {
    const result = await api.post<AgendamentoResponse>(`/api/agendamentos/${id}/confirmar`, {})
    setAgendamento(result)
  }, [])

  const concluir = useCallback(async (id: string) => {
    return api.post<ConcluirResponse>(`/api/agendamentos/${id}/concluir`, {})
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<AgendamentoResponse>(`/api/agendamentos/${id}/cancelar`, {})
    setAgendamento(result)
  }, [])

  const slots = useCallback(async (profissionalId: string, data: string, servicoId: string) => {
    return api.get<string[]>(
      `/api/agendamentos/slots?profissionalId=${profissionalId}&data=${data}&servicoId=${servicoId}`
    )
  }, [])

  return {
    agendamentos,
    agendamento,
    loading,
    error,
    list,
    listSemana,
    get,
    create,
    confirmar,
    concluir,
    cancelar,
    slots,
  }
}
