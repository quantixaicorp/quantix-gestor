import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  KpisGeralResponse, RelatorioVendasResponse,
  RelatorioFinanceiroResponse, RelatorioEstoqueResponse, RelatorioClientesResponse,
} from '@/types/relatorios'

export function useRelatorios() {
  const [kpis, setKpis] = useState<KpisGeralResponse | null>(null)
  const [vendas, setVendas] = useState<RelatorioVendasResponse | null>(null)
  const [financeiro, setFinanceiro] = useState<RelatorioFinanceiroResponse | null>(null)
  const [estoque, setEstoque] = useState<RelatorioEstoqueResponse | null>(null)
  const [clientes, setClientes] = useState<RelatorioClientesResponse | null>(null)
  const [loading, setLoading] = useState(false)

  function buildQs(de: string, ate: string) {
    return `?de=${encodeURIComponent(de)}&ate=${encodeURIComponent(ate)}`
  }

  const loadKpis = useCallback(async (de: string, ate: string) => {
    setLoading(true)
    try {
      const [k, v, f, e, c] = await Promise.all([
        api.get<KpisGeralResponse>(`/api/relatorios/kpis${buildQs(de, ate)}`),
        api.get<RelatorioVendasResponse>(`/api/relatorios/vendas${buildQs(de, ate)}`),
        api.get<RelatorioFinanceiroResponse>(`/api/relatorios/financeiro${buildQs(de, ate)}`),
        api.get<RelatorioEstoqueResponse>(`/api/relatorios/estoque${buildQs(de, ate)}`),
        api.get<RelatorioClientesResponse>(`/api/relatorios/clientes${buildQs(de, ate)}`),
      ])
      setKpis(k); setVendas(v); setFinanceiro(f); setEstoque(e); setClientes(c)
    } finally {
      setLoading(false)
    }
  }, [])

  return { kpis, vendas, financeiro, estoque, clientes, loading, loadKpis }
}
