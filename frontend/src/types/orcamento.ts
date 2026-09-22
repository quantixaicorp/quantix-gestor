export interface OrcamentoItemRequest {
  type: 'Produto' | 'Livre'
  productId?: string
  description: string
  quantity: number
  unitPrice: number
}

export interface CreateOrcamentoRequest {
  customerId?: string
  title: string
  expirationDate: string
  notes?: string
  items: OrcamentoItemRequest[]
}

export interface OrcamentoItemResponse {
  id: string
  type: 'Produto' | 'Livre'
  productId: string | null
  description: string
  quantity: number
  unitPrice: number
}

export interface OrcamentoResponse {
  id: string
  number: number
  title: string
  customerId: string | null
  customerName: string | null
  clienteWhatsapp: string | null
  expirationDate: string
  status: OrcamentoStatus
  notes: string | null
  saleId: string | null
  publicToken: string | null
  createdAt: string
  items: OrcamentoItemResponse[]
  total: number
}

export interface OrcamentoListItem {
  id: string
  number: number
  title: string
  customerName: string | null
  expirationDate: string
  status: OrcamentoStatus
  total: number
}

export type OrcamentoStatus =
  | 'Rascunho'
  | 'Enviado'
  | 'Aprovado'
  | 'Convertido'
  | 'Rejeitado'
  | 'Cancelado'
  | 'Expirado'
