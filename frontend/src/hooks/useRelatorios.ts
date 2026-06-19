import { useState, useCallback, useRef } from 'react'
import { api } from '@/services/api'
import type {
  KpisGeralResponse, RelatorioVendasResponse,
  RelatorioFinanceiroResponse, RelatorioEstoqueResponse, RelatorioClientesResponse,
  CurvaAbcResponse, DreResponse, RelatorioTabId,
  RelatorioAgendamentosResponse, RelatorioContratosResponse,
  RelatorioCobrancasResponse, RelatorioOrcamentosResponse, RelatorioAssinaturasResponse,
} from '@/types/relatorios'

type TabData = {
  'visao-geral': KpisGeralResponse
  'vendas': RelatorioVendasResponse
  'financeiro': RelatorioFinanceiroResponse
  'estoque': RelatorioEstoqueResponse
  'clientes': RelatorioClientesResponse
  'agendamentos': RelatorioAgendamentosResponse
  'contratos': RelatorioContratosResponse
  'cobrancas': RelatorioCobrancasResponse
  'orcamentos': RelatorioOrcamentosResponse
  'assinaturas': RelatorioAssinaturasResponse
  'curva-abc': { produtos: CurvaAbcResponse; clientes: CurvaAbcResponse }
  'dre': DreResponse
}

export type RelatorioData = Partial<TabData>

export function useRelatorios() {
  const [data, setData] = useState<RelatorioData>({})
  const [loadingTab, setLoadingTab] = useState<RelatorioTabId | null>(null)
  const periodoRef = useRef({ de: '', ate: '', tipoData: 'pagamento' })
  const loadedRef = useRef<Set<RelatorioTabId>>(new Set())

  function buildQs(de: string, ate: string, extra?: Record<string, string>) {
    const qs = new URLSearchParams({ de, ate, ...(extra ?? {}) })
    return `?${qs}`
  }

  const setPeriodo = useCallback((de: string, ate: string, tipoData?: string) => {
    periodoRef.current = { de, ate, tipoData: tipoData ?? 'pagamento' }
    loadedRef.current.clear()
    setData({})
  }, [])

  const loadTab = useCallback(async (tab: RelatorioTabId) => {
    const { de, ate } = periodoRef.current
    if (!de || !ate) return
    if (loadedRef.current.has(tab)) return

    setLoadingTab(tab)
    try {
      const qs = buildQs(de, ate, {})
      switch (tab) {
        case 'visao-geral': {
          const d = await api.get<KpisGeralResponse>(`/api/relatorios/kpis${qs}`)
          setData(prev => ({ ...prev, 'visao-geral': d }))
          break
        }
        case 'vendas': {
          const d = await api.get<RelatorioVendasResponse>(`/api/relatorios/vendas${qs}`)
          setData(prev => ({ ...prev, vendas: d }))
          break
        }
        case 'financeiro': {
          const finQs = buildQs(de, ate, { tipoData: periodoRef.current.tipoData })
          const d = await api.get<RelatorioFinanceiroResponse>(`/api/relatorios/financeiro${finQs}`)
          setData(prev => ({ ...prev, financeiro: d }))
          break
        }
        case 'estoque': {
          const d = await api.get<RelatorioEstoqueResponse>(`/api/relatorios/estoque${qs}`)
          setData(prev => ({ ...prev, estoque: d }))
          break
        }
        case 'clientes': {
          const d = await api.get<RelatorioClientesResponse>(`/api/relatorios/clientes${qs}`)
          setData(prev => ({ ...prev, clientes: d }))
          break
        }
        case 'agendamentos': {
          const d = await api.get<RelatorioAgendamentosResponse>(`/api/relatorios/agendamentos${qs}`)
          setData(prev => ({ ...prev, agendamentos: d }))
          break
        }
        case 'contratos': {
          const d = await api.get<RelatorioContratosResponse>(`/api/relatorios/contratos`)
          setData(prev => ({ ...prev, contratos: d }))
          break
        }
        case 'cobrancas': {
          const d = await api.get<RelatorioCobrancasResponse>(`/api/relatorios/cobrancas`)
          setData(prev => ({ ...prev, cobrancas: d }))
          break
        }
        case 'orcamentos': {
          const d = await api.get<RelatorioOrcamentosResponse>(`/api/relatorios/orcamentos${qs}`)
          setData(prev => ({ ...prev, orcamentos: d }))
          break
        }
        case 'assinaturas': {
          const d = await api.get<RelatorioAssinaturasResponse>(`/api/relatorios/assinaturas${qs}`)
          setData(prev => ({ ...prev, assinaturas: d }))
          break
        }
        case 'curva-abc': {
          const [produtos, clientes] = await Promise.all([
            api.get<CurvaAbcResponse>(`/api/relatorios/curva-abc/produtos${qs}`),
            api.get<CurvaAbcResponse>(`/api/relatorios/curva-abc/clientes${qs}`),
          ])
          setData(prev => ({ ...prev, 'curva-abc': { produtos, clientes } }))
          break
        }
        case 'dre': {
          const d = await api.get<DreResponse>(`/api/relatorios/dre${qs}`)
          setData(prev => ({ ...prev, dre: d }))
          break
        }
      }
      loadedRef.current.add(tab)
    } finally {
      setLoadingTab(null)
    }
  }, [])

  return { data, loadingTab, setPeriodo, loadTab }
}
