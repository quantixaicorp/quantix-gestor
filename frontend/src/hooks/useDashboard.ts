import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { DashboardResponse } from '@/types/dashboard'

export function useDashboard() {
  const [data, setData] = useState<DashboardResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await api.get<DashboardResponse>('/api/dashboard')
      setData(result)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar dashboard')
    } finally {
      setLoading(false)
    }
  }, [])

  return { data, loading, error, load }
}
