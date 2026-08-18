export type StatusCompra = 'Rascunho' | 'Confirmada' | 'Cancelada'
export type StatusPedidoCompra =
  | 'Rascunho' | 'AguardandoAprovacao' | 'Aprovado'
  | 'RecebidoParcialmente' | 'RecebidoTotalmente' | 'Cancelado'
export type DestinoCompra = 'EstoqueParaVenda' | 'ConsumoInterno' | 'AtivoImobilizado'

export interface ItemCompraRequest {
  productId?: string
  description: string
  destination: DestinoCompra
  quantity: number
  unitPrice: number
  discount: number
  allocatedFreight: number
  taxes: number
  financialCategory?: string
  costCenter?: string
}

export interface ParcelaPersonalizadaRequest {
  numero: number
  dataVencimento: string
  valor: number
}

export interface CreateCompraRequest {
  supplierId: string
  date: string
  purchaseType: string
  noteNumber?: string
  paymentTerms: string
  paymentMethod: string
  installmentCount?: number
  parcelasPersonalizadas?: ParcelaPersonalizadaRequest[]
  purchaseOrderId?: string
  notes?: string
  items: ItemCompraRequest[]
}

export interface UpdateCompraRequest extends CreateCompraRequest {}

export interface ItemCompraResponse {
  id: string
  productId?: string
  description: string
  destination: string
  quantity: number
  unitPrice: number
  discount: number
  allocatedFreight: number
  taxes: number
  totalAmount: number
  financialCategory?: string
  costCenter?: string
}

export interface ParcelamentoResumo {
  id: string
  description: string
  totalAmount: number
  installmentCount: number
  status: string
}

export interface CompraResponse {
  id: string
  number: number
  date: string
  supplierId: string
  fornecedorNome: string
  purchaseOrderId?: string
  purchaseType: string
  noteNumber?: string
  paymentTerms: string
  paymentMethod: string
  status: StatusCompra
  totalAmount: number
  notes?: string
  createdAt: string
  items: ItemCompraResponse[]
  installmentPlan?: ParcelamentoResumo
}

export interface CompraResumoResponse {
  totalCompraMes: number
  qtdComprasMes: number
  totalContasPagarGeradas: number
}

export interface ItemPedidoCompraRequest {
  productId?: string
  description: string
  quantity: number
  estimatedAmount: number
}

export interface CreatePedidoCompraRequest {
  supplierId: string
  date: string
  notes?: string
  items: ItemPedidoCompraRequest[]
}

export interface UpdatePedidoCompraRequest extends CreatePedidoCompraRequest {}

export interface ItemPedidoCompraResponse {
  id: string
  productId?: string
  description: string
  quantity: number
  estimatedAmount: number
}

export interface PedidoCompraResponse {
  id: string
  number: number
  date: string
  supplierId: string
  fornecedorNome: string
  status: StatusPedidoCompra
  estimatedAmount: number
  notes?: string
  createdAt: string
  items: ItemPedidoCompraResponse[]
}
