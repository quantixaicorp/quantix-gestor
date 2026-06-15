import { useCallback, useState } from 'react'
import { api } from '@/services/api'
import type { DashboardLayoutDto, WidgetId } from '@/types/dashboard'

const DEFAULT_LAYOUT: WidgetId[] = [
  'kpi-saldo-mes',
  'kpi-vendas-hoje',
  'kpi-contas-vencidas',
  'kpi-contas-receber',
  'grafico-tendencia-vendas',
  'grafico-fluxo-caixa',
  'tabela-top-produtos',
  'alerta-estoque-baixo',
]

export function useDashboardLayout() {
  const [widgets, setWidgets] = useState<WidgetId[]>(DEFAULT_LAYOUT)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<DashboardLayoutDto>('/api/dashboard/layout')
      if (data.widgets.length > 0) setWidgets(data.widgets)
    } catch {
      // keep default on error
    } finally {
      setLoading(false)
    }
  }, [])

  const save = useCallback(async (newWidgets: WidgetId[]) => {
    await api.put('/api/dashboard/layout', { widgets: newWidgets })
    setWidgets(newWidgets)
  }, [])

  return { widgets, loading, load, save, setWidgets }
}
