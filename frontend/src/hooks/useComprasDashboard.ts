import { useState, useCallback } from 'react'
import { api } from '@/services/api'

export interface ComprasMensalSerieItem { mes: string; total: number; quantidade: number }
export interface ComprasPorFornecedorItem { fornecedor: string; total: number }
export interface TopProdutoCompradoItem { produto: string; quantidadeTotal: number; valorTotal: number }

export interface ComprasDashboardData {
  totalMes: number
  totalAno: number
  ticketMedio: number
  qtdComprasMes: number
  fornecedoresAtivos: number
  seriesMensal: ComprasMensalSerieItem[]
  porFornecedor: ComprasPorFornecedorItem[]
  topProdutos: TopProdutoCompradoItem[]
}

export function useComprasDashboard() {
  const [data, setData] = useState<ComprasDashboardData | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async (de: string, ate: string) => {
    setLoading(true)
    try {
      const result = await api.get<ComprasDashboardData>(
        `/api/compras/dashboard?de=${de}&ate=${ate}`
      )
      setData(result)
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, load }
}
