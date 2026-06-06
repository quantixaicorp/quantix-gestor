export interface ItemVendaRequest {
  produtoId: string
  quantidade: number
  desconto: number
}

export interface CreateVendaRequest {
  clienteId?: string
  itens: ItemVendaRequest[]
  desconto: number
  formaPagamento: 'Dinheiro' | 'Pix' | 'Cartao' | 'Outro'
  parcelas?: number
  observacao?: string
  dataHora?: string
}

export interface ItemVendaResponse {
  produtoId: string
  produtoNome: string
  quantidade: number
  precoUnitario: number
  desconto: number
  total: number
}

export interface VendaResponse {
  id: string
  clienteNome: string | null
  dataHora: string
  status: string
  subtotal: number
  desconto: number
  total: number
  formaPagamento: string
  parcelas: number | null
  observacao: string | null
  itens: ItemVendaResponse[]
}

export interface VendaListItem {
  id: string
  clienteNome: string | null
  dataHora: string
  status: string
  total: number
  formaPagamento: string
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
  formaPagamento: 'Dinheiro' | 'Pix' | 'Cartao' | 'Outro'
  parcelas?: number
  observacao?: string
}
