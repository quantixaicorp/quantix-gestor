import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { ClienteResponse, CreateClienteRequest, UpdateClienteRequest } from '@/types/clientes'

export function useClientes() {
  const [clientes, setClientes] = useState<ClienteResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (busca?: string) => {
    setLoading(true)
    try {
      const qs = busca ? `?busca=${encodeURIComponent(busca)}` : ''
      const data = await api.get<ClienteResponse[]>(`/api/clientes${qs}`)
      setClientes(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar clientes')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateClienteRequest) => {
    const result = await api.post<ClienteResponse>('/api/clientes', req)
    setClientes(prev => [...prev, result])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateClienteRequest) => {
    const result = await api.put<ClienteResponse>(`/api/clientes/${id}`, req)
    setClientes(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/clientes/${id}`)
    setClientes(prev => prev.filter(c => c.id !== id))
  }, [])

  return { clientes, loading, error, list, create, update, remove }
}
