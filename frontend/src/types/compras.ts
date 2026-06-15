export type StatusCompra = 'Rascunho' | 'Confirmada' | 'Cancelada'
export type StatusPedidoCompra =
  | 'Rascunho' | 'AguardandoAprovacao' | 'Aprovado'
  | 'RecebidoParcialmente' | 'RecebidoTotalmente' | 'Cancelado'
export type DestinoCompra = 'EstoqueParaVenda' | 'ConsumoInterno' | 'AtivoImobilizado'

export interface ItemCompraRequest {
  produtoId?: string
  descricao: string
  destinoCompra: DestinoCompra
  quantidade: number
  valorUnitario: number
  desconto: number
  freteRateado: number
  impostos: number
  categoriaFinanceira?: string
  centroCusto?: string
}

export interface ParcelaPersonalizadaRequest {
  numero: number
  dataVencimento: string
  valor: number
}

export interface CreateCompraRequest {
  fornecedorId: string
  data: string
  tipoCompra: string
  numeroNota?: string
  condicaoPagamento: string
  formaPagamento: string
  qtdParcelas?: number
  parcelasPersonalizadas?: ParcelaPersonalizadaRequest[]
  pedidoCompraId?: string
  observacoes?: string
  itens: ItemCompraRequest[]
}

export interface UpdateCompraRequest extends CreateCompraRequest {}

export interface ItemCompraResponse {
  id: string
  produtoId?: string
  descricao: string
  destinoCompra: string
  quantidade: number
  valorUnitario: number
  desconto: number
  freteRateado: number
  impostos: number
  valorTotal: number
  categoriaFinanceira?: string
  centroCusto?: string
}

export interface ParcelamentoResumo {
  id: string
  descricao: string
  valorTotal: number
  qtdParcelas: number
  status: string
}

export interface CompraResponse {
  id: string
  numero: number
  data: string
  fornecedorId: string
  fornecedorNome: string
  pedidoCompraId?: string
  tipoCompra: string
  numeroNota?: string
  condicaoPagamento: string
  formaPagamento: string
  status: StatusCompra
  valorTotal: number
  observacoes?: string
  criadaEm: string
  itens: ItemCompraResponse[]
  parcelamento?: ParcelamentoResumo
}

export interface CompraResumoResponse {
  totalCompraMes: number
  qtdComprasMes: number
  totalContasPagarGeradas: number
}

export interface ItemPedidoCompraRequest {
  produtoId?: string
  descricao: string
  quantidade: number
  valorEstimado: number
}

export interface CreatePedidoCompraRequest {
  fornecedorId: string
  data: string
  observacoes?: string
  itens: ItemPedidoCompraRequest[]
}

export interface UpdatePedidoCompraRequest extends CreatePedidoCompraRequest {}

export interface ItemPedidoCompraResponse {
  id: string
  produtoId?: string
  descricao: string
  quantidade: number
  valorEstimado: number
}

export interface PedidoCompraResponse {
  id: string
  numero: number
  data: string
  fornecedorId: string
  fornecedorNome: string
  status: StatusPedidoCompra
  valorEstimado: number
  observacoes?: string
  criadoEm: string
  itens: ItemPedidoCompraResponse[]
}
