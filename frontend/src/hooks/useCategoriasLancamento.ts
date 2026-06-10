import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  CategoriaLancamentoResponse,
  CreateCategoriaLancamentoRequest,
  UpdateCategoriaLancamentoRequest,
} from '@/types/financeiro'

export function useCategoriasLancamento() {
  const list = useCallback(async (tipo?: string): Promise<CategoriaLancamentoResponse[]> => {
    const qs = tipo ? `?tipo=${tipo}` : ''
    return api.get<CategoriaLancamentoResponse[]>(`/api/categorias-lancamento${qs}`)
  }, [])

  const create = useCallback(async (req: CreateCategoriaLancamentoRequest) =>
    api.post<CategoriaLancamentoResponse>('/api/categorias-lancamento', req), [])

  const update = useCallback(async (id: string, req: UpdateCategoriaLancamentoRequest) =>
    api.put<CategoriaLancamentoResponse>(`/api/categorias-lancamento/${id}`, req), [])

  const remove = useCallback(async (id: string) =>
    api.delete(`/api/categorias-lancamento/${id}`), [])

  return { list, create, update, remove }
}
