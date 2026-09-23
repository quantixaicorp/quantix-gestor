export type AccountType = 'Ativo' | 'Passivo' | 'PatrimonioLiquido' | 'Receita' | 'Despesa'
export type AccountingSystem = 'Dominio' | 'Fortes'

export interface ChartOfAccountResponse {
  id: string
  code: string
  name: string
  type: AccountType
  parentId: string | null
  isActive: boolean
  children: ChartOfAccountResponse[]
}

export interface CreateChartOfAccountRequest {
  code: string
  name: string
  type: AccountType
  parentId: string | null
}

export interface UpdateChartOfAccountRequest {
  code: string
  name: string
}

export interface AccountMappingResponse {
  id: string
  categoryName: string
  accountId: string
  accountCode: string
  accountName: string
}

export interface AccountMappingItem {
  categoryName: string
  accountId: string
}

export interface AccountingSettingsResponse {
  defaultCashAccountId: string | null
  defaultCashAccountCode: string | null
  defaultCashAccountName: string | null
  preferredAccountingSystem: AccountingSystem | null
}

export interface UpdateAccountingSettingsRequest {
  defaultCashAccountId: string | null
  preferredAccountingSystem: AccountingSystem | null
}

export interface AccountingExportRequest {
  system: AccountingSystem
  months: string[]       // ["2025-01", "2025-02"]
  includePending: boolean
}
