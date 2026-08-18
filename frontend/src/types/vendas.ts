export interface ItemVendaRequest {
  productId: string
  quantity: number
  discount: number
}

export interface CreateVendaRequest {
  customerId?: string
  items: ItemVendaRequest[]
  discount: number
  paymentMethod: 'Dinheiro' | 'Pix' | 'Cartao' | 'Outro'
  installments?: number
  notes?: string
  saleDate?: string
  professionalId?: string
  serviceOrderNotes?: string
}

export interface ItemVendaResponse {
  productId: string
  produtoNome: string
  quantity: number
  unitPrice: number
  discount: number
  total: number
}

export interface UpdateVendaRequest {
  customerId?: string | null
  paymentMethod: string
  saleDate: string
}

export interface VendaResponse {
  id: string
  customerId: string | null
  customerName: string | null
  saleDate: string
  status: string
  subtotal: number
  discount: number
  total: number
  paymentMethod: string
  installments: number | null
  notes: string | null
  items: ItemVendaResponse[]
  professionalName?: string | null
  serviceOrderNotes?: string | null
}

export interface VendaListItem {
  id: string
  customerId: string | null
  customerName: string | null
  saleDate: string
  status: string
  total: number
  paymentMethod: string
  professionalName?: string | null
}

export interface ItemCarrinho {
  produtoId: string
  produtoNome: string
  precoUnitario: number
  quantidade: number
  desconto: number
  total: number
}

export interface FecharVendaRequest {
  paymentMethod: 'Dinheiro' | 'Pix' | 'Cartao' | 'Outro'
  installments?: number
  notes?: string
}
