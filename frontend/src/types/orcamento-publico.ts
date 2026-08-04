export interface OrcamentoItemPublico {
  description: string
  quantity: number
  unitPrice: number
  total: number
}

export interface OrcamentoPublico {
  title: string
  customerName: string | null
  expirationDate: string
  status: string
  notes: string | null
  items: OrcamentoItemPublico[]
  total: number
}
