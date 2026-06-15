import { useCallback, useState } from 'react'
import { api } from '@/services/api'
import type { DashboardExtrasResponse } from '@/types/dashboard'

export function useDashboardExtras() {
  const [data, setData] = useState<DashboardExtrasResponse | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.get<DashboardExtrasResponse>('/api/dashboard/extras')
      setData(result)
    } catch {
      // extras indisponíveis não quebram o dashboard principal
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, load }
}
