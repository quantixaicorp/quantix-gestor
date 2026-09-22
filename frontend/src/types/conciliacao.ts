export interface BankAccountResponse {
  id: string
  name: string
  bankName: string
  accountNumber: string | null
  agency: string | null
  isActive: boolean
}

export interface CreateBankAccountRequest {
  name: string
  bankName: string
  accountNumber?: string
  agency?: string
}

export interface BankReconciliationResponse {
  id: string
  transactionId: string
  transactionDescription: string
  transactionAmount: number
  transactionDueDate: string
  confidenceScore: number
  matchType: 'Auto' | 'Manual'
  createdByImport: boolean
}

export interface BankStatementItemResponse {
  id: string
  date: string
  amount: number
  description: string
  bankTransactionId: string | null
  status: 'AutoConciliated' | 'PendingReview' | 'ManuallyIgnored' | 'Unmatched'
  reconciliation: BankReconciliationResponse | null
}

export interface BankStatementListItem {
  id: string
  bankAccountId: string
  bankAccountName: string
  fileName: string
  format: 'OFX' | 'CSV'
  periodStart: string
  periodEnd: string
  importedAt: string
  itemCount: number
  autoConciliated: number
  pendingReview: number
  unmatched: number
  manuallyIgnored: number
}
