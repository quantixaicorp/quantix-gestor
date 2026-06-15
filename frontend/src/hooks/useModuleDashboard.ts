import { useCallback, useState } from 'react'
import { api } from '@/services/api'
import type { ModulosDashboardResponse } from '@/types/dashboard'

export function useModuleDashboard() {
  const [data, setData] = useState<ModulosDashboardResponse | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.get<ModulosDashboardResponse>('/api/dashboard/modulos')
      setData(result)
    } catch {
      // módulos indisponíveis não quebram o dashboard principal
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, load }
}
