import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { FornecedorResponse, CreateFornecedorRequest, UpdateFornecedorRequest } from '@/types/fornecedores'

export function useFornecedores() {
  const [fornecedores, setFornecedores] = useState<FornecedorResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (busca?: string) => {
    setLoading(true)
    try {
      const qs = busca ? `?busca=${encodeURIComponent(busca)}` : ''
      const data = await api.get<FornecedorResponse[]>(`/api/fornecedores${qs}`)
      setFornecedores(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar fornecedores')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateFornecedorRequest) => {
    const result = await api.post<FornecedorResponse>('/api/fornecedores', req)
    setFornecedores(prev => [...prev, result].sort((a, b) => a.nome.localeCompare(b.nome)))
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateFornecedorRequest) => {
    const result = await api.put<FornecedorResponse>(`/api/fornecedores/${id}`, req)
    setFornecedores(prev => prev.map(f => f.id === id ? result : f))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/fornecedores/${id}`)
    setFornecedores(prev => prev.filter(f => f.id !== id))
  }, [])

  return { fornecedores, loading, error, list, create, update, remove }
}
