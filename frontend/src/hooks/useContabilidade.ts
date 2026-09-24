import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ChartOfAccountResponse,
  CreateChartOfAccountRequest,
  UpdateChartOfAccountRequest,
  AccountMappingResponse,
  AccountMappingItem,
  AccountingSettingsResponse,
  UpdateAccountingSettingsRequest,
  AccountingExportRequest,
} from '@/types/contabilidade'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

export function useContabilidade() {
  const listAccounts = useCallback(() =>
    api.get<ChartOfAccountResponse[]>('/api/chart-of-accounts'), [])

  const createAccount = useCallback((req: CreateChartOfAccountRequest) =>
    api.post<ChartOfAccountResponse>('/api/chart-of-accounts', req), [])

  const updateAccount = useCallback((id: string, req: UpdateChartOfAccountRequest) =>
    api.put<ChartOfAccountResponse>(`/api/chart-of-accounts/${id}`, req), [])

  const deleteAccount = useCallback((id: string) =>
    api.delete(`/api/chart-of-accounts/${id}`), [])

  const loadTemplate = useCallback(() =>
    api.post('/api/chart-of-accounts/load-template', {}), [])

  const listMappings = useCallback(() =>
    api.get<AccountMappingResponse[]>('/api/account-mappings'), [])

  const saveMappings = useCallback((mappings: AccountMappingItem[]) =>
    api.put('/api/account-mappings', { mappings }), [])

  const getSettings = useCallback(() =>
    api.get<AccountingSettingsResponse>('/api/accounting-settings'), [])

  const updateSettings = useCallback((req: UpdateAccountingSettingsRequest) =>
    api.put<AccountingSettingsResponse>('/api/accounting-settings', req), [])

  const downloadExport = useCallback(async (req: AccountingExportRequest) => {
    const token = localStorage.getItem('ga_token')
    const res = await fetch(`${API_BASE}/api/accounting-export/download`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(req),
    })
    if (!res.ok) {
      const text = await res.text().catch(() => '')
      let msg = text
      try {
        const body = JSON.parse(text)
        if (body.error) msg = body.error
        else if (body.title) msg = body.title
        else if (body.errors) msg = (Object.values(body.errors) as string[][]).flat().join('; ') || body.title || 'Erro de validação'
      } catch { /* plain text */ }
      throw new Error(msg || `Erro ${res.status}`)
    }
    const blob = await res.blob()
    const disposition = res.headers.get('Content-Disposition') ?? ''
    const match = disposition.match(/filename="?([^"]+)"?/)
    const fileName = match?.[1] ?? 'export.txt'
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    a.click()
    URL.revokeObjectURL(url)
  }, [])

  return {
    listAccounts, createAccount, updateAccount, deleteAccount, loadTemplate,
    listMappings, saveMappings,
    getSettings, updateSettings,
    downloadExport,
  }
}
