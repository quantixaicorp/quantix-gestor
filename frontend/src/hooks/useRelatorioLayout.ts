import { useCallback, useState } from 'react'
import { api } from '@/services/api'
import type { RelatorioLayoutDto, RelatorioTabId } from '@/types/relatorios'

const DEFAULT_TABS: RelatorioTabId[] = [
  'visao-geral',
  'vendas',
  'financeiro',
  'estoque',
  'clientes',
  'agendamentos',
  'contratos',
  'cobrancas',
  'orcamentos',
  'assinaturas',
  'curva-abc',
  'dre',
]

export function useRelatorioLayout() {
  const [tabs, setTabs] = useState<RelatorioTabId[]>(DEFAULT_TABS)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<RelatorioLayoutDto>('/api/relatorios/layout')
      if (data.tabs.length > 0) setTabs(data.tabs as RelatorioTabId[])
    } catch {
      // keep default on error
    } finally {
      setLoading(false)
    }
  }, [])

  const save = useCallback(async (newTabs: RelatorioTabId[]) => {
    await api.put('/api/relatorios/layout', { tabs: newTabs })
    setTabs(newTabs)
  }, [])

  return { tabs, loading, load, save }
}
