import { useCallback } from 'react'
import { api } from '@/services/api'
import type { ParcelamentoResponse } from '@/types/parcelamentos'

export function useParcelamentos() {
  const get = useCallback(
    (id: string) => api.get<ParcelamentoResponse>(`/api/parcelamentos/${id}`),
    []
  )

  const listByCompra = useCallback(
    (compraId: string) =>
      api.get<ParcelamentoResponse[]>(`/api/parcelamentos?compraId=${compraId}`),
    []
  )

  return { get, listByCompra }
}
