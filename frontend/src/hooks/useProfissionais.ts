import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ProfissionalResponse, DisponibilidadeItem,
  DisponibilidadePeriodoResponse, SalvarDisponibilidadeRequest,
  BloqueioResponse,
} from '@/types/agendamento'

interface CriarProfissionalRequest { nome: string; telefone?: string }
interface AtualizarProfissionalRequest { nome: string; telefone?: string; ativo: boolean }
interface CriarBloqueioRequest {
  profissionalId?: string
  dataInicio: string
  dataFim: string
  motivo?: string
}

export function useProfissionais() {
  const [profissionais, setProfissionais] = useState<ProfissionalResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<ProfissionalResponse[]>('/api/profissionais')
      setProfissionais(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar profissionais')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CriarProfissionalRequest) => {
    const result = await api.post<ProfissionalResponse>('/api/profissionais', req)
    setProfissionais(prev => [...prev, result])
    return result
  }, [])

  const update = useCallback(async (id: string, req: AtualizarProfissionalRequest) => {
    const result = await api.put<ProfissionalResponse>(`/api/profissionais/${id}`, req)
    setProfissionais(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/profissionais/${id}`)
    setProfissionais(prev => prev.filter(p => p.id !== id))
  }, [])

  const listPeriodos = useCallback(async (id: string) => {
    return api.get<DisponibilidadePeriodoResponse[]>(`/api/profissionais/${id}/disponibilidade`)
  }, [])

  const getDisponibilidade = useCallback(async (id: string, dataInicio: string, dataFim: string) => {
    return api.get<DisponibilidadePeriodoResponse>(
      `/api/profissionais/${id}/disponibilidade/periodo?dataInicio=${dataInicio}&dataFim=${dataFim}`)
  }, [])

  const saveDisponibilidade = useCallback(async (id: string, req: SalvarDisponibilidadeRequest) => {
    await api.put(`/api/profissionais/${id}/disponibilidade`, req)
  }, [])

  const deletePeriodo = useCallback(async (id: string, dataInicio: string, dataFim: string) => {
    await api.delete(`/api/profissionais/${id}/disponibilidade/periodo?dataInicio=${dataInicio}&dataFim=${dataFim}`)
  }, [])

  const listBloqueios = useCallback(async (de: string, ate: string) => {
    return api.get<BloqueioResponse[]>(`/api/agenda/bloqueios?de=${de}&ate=${ate}`)
  }, [])

  const criarBloqueio = useCallback(async (req: CriarBloqueioRequest) => {
    return api.post<BloqueioResponse>('/api/agenda/bloqueios', req)
  }, [])

  const deleteBloqueio = useCallback(async (id: string) => {
    await api.delete(`/api/agenda/bloqueios/${id}`)
  }, [])

  return {
    profissionais,
    loading,
    error,
    list,
    create,
    update,
    remove,
    listPeriodos,
    getDisponibilidade,
    saveDisponibilidade,
    deletePeriodo,
    listBloqueios,
    criarBloqueio,
    deleteBloqueio,
  }
}
