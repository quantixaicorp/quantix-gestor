export type CobrancaStatus = 'Pendente' | 'Pago' | 'Cancelado' | 'Vencido'

export interface CobrancaResponse {
  id: string
  clienteNome: string
  clienteWhatsapp: string
  contratoId: string | null
  contratoTitulo: string | null
  referencia: string
  valor: number
  dataVencimento: string
  dataPagamento: string | null
  status: CobrancaStatus
  formaPagamento: string | null
  observacao: string | null
  criadoEm: string
}

export interface CobrancaListItem {
  id: string
  clienteNome: string
  contratoId: string | null
  contratoTitulo: string | null
  referencia: string
  valor: number
  dataVencimento: string
  status: CobrancaStatus
}

export interface CreateCobrancaRequest {
  clienteId: string
  referencia: string
  valor: number
  dataVencimento: string
  observacao?: string
}

export interface PagarCobrancaRequest {
  dataPagamento: string
  formaPagamento: string
}
