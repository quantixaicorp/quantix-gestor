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
  asaasId: string | null
  asaasPaymentLink: string | null
  asaasPixQrCode: string | null
  asaasBoletoUrl: string | null
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

export interface CobrancaAsaasResponse {
  asaasId: string
  paymentLink: string | null
  pixQrCode: string | null
  boletoUrl: string | null
}

export interface AgingData {
  atual: number
  ate30Dias: number
  de31A60Dias: number
  de61A90Dias: number
  acima90Dias: number
  total: number
  qtdAtual: number
  qtdAte30Dias: number
  qtdDe31A60Dias: number
  qtdDe61A90Dias: number
  qtdAcima90Dias: number
}
