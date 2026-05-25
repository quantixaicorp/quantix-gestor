import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ProdutoResponse, CategoriaResponse, MovimentacaoResponse,
  CreateProdutoRequest, UpdateProdutoRequest, EntradaEstoqueRequest,
} from '@/types/estoque'

export function useEstoque() {
  const [produtos, setProdutos] = useState<ProdutoResponse[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [movimentacoes, setMovimentacoes] = useState<MovimentacaoResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const listProdutos = useCallback(async (params?: {
    busca?: string; categoriaId?: string; estoqueBaixo?: boolean
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (params?.busca) qs.set('busca', params.busca)
      if (params?.categoriaId) qs.set('categoriaId', params.categoriaId)
      if (params?.estoqueBaixo) qs.set('estoqueBaixo', 'true')
      const data = await api.get<ProdutoResponse[]>(`/api/produtos?${qs}`)
      setProdutos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar produtos')
    } finally {
      setLoading(false)
    }
  }, [])

  const listCategorias = useCallback(async () => {
    const data = await api.get<CategoriaResponse[]>('/api/categorias')
    setCategorias(data)
  }, [])

  const createProduto = useCallback(async (req: CreateProdutoRequest) => {
    const result = await api.post<ProdutoResponse>('/api/produtos', req)
    setProdutos(prev => [...prev, result])
    return result
  }, [])

  const updateProduto = useCallback(async (id: string, req: UpdateProdutoRequest) => {
    const result = await api.put<ProdutoResponse>(`/api/produtos/${id}`, req)
    setProdutos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const entradaEstoque = useCallback(async (req: EntradaEstoqueRequest) => {
    const result = await api.post<ProdutoResponse>('/api/estoque/movimentar', req)
    setProdutos(prev => prev.map(p => p.id === req.produtoId ? result : p))
    return result
  }, [])

  const listMovimentacoes = useCallback(async (produtoId?: string) => {
    const qs = produtoId ? `?produtoId=${produtoId}` : ''
    const data = await api.get<MovimentacaoResponse[]>(`/api/estoque/movimentacoes${qs}`)
    setMovimentacoes(data)
  }, [])

  return {
    produtos, categorias, movimentacoes, loading, error,
    listProdutos, listCategorias, createProduto, updateProduto,
    entradaEstoque, listMovimentacoes,
  }
}
