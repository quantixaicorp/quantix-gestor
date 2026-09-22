import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  BankAccountResponse,
  BankStatementItemResponse,
  BankStatementListItem,
  CreateBankAccountRequest,
} from '@/types/conciliacao'

export function useConciliacao() {
  const listAccounts = useCallback(() =>
    api.get<BankAccountResponse[]>('/api/bank-accounts'), [])

  const createAccount = useCallback((req: CreateBankAccountRequest) =>
    api.post<BankAccountResponse>('/api/bank-accounts', req), [])

  const deleteAccount = useCallback((id: string) =>
    api.delete(`/api/bank-accounts/${id}`), [])

  const importStatement = useCallback((file: File, bankAccountId: string) => {
    const form = new FormData()
    form.append('file', file)
    form.append('bankAccountId', bankAccountId)
    return api.postForm<BankStatementListItem>('/api/bank-statements/import', form)
  }, [])

  const listStatements = useCallback((bankAccountId?: string) => {
    const qs = bankAccountId ? `?bankAccountId=${bankAccountId}` : ''
    return api.get<BankStatementListItem[]>(`/api/bank-statements${qs}`)
  }, [])

  const getStatementItems = useCallback((id: string) =>
    api.get<BankStatementItemResponse[]>(`/api/bank-statements/${id}/items`), [])

  const deleteStatement = useCallback((id: string) =>
    api.delete(`/api/bank-statements/${id}`), [])

  const manualMatch = useCallback((itemId: string, transactionId: string) =>
    api.post('/api/bank-reconciliation/match', { itemId, transactionId }), [])

  const undoMatch = useCallback((reconciliationId: string) =>
    api.delete(`/api/bank-reconciliation/${reconciliationId}`), [])

  const ignoreItem = useCallback((itemId: string) =>
    api.post(`/api/bank-reconciliation/${itemId}/ignore`, {}), [])

  return {
    listAccounts, createAccount, deleteAccount,
    importStatement, listStatements, getStatementItems, deleteStatement,
    manualMatch, undoMatch, ignoreItem,
  }
}
