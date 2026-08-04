export type CobrancaStatus = 'Pendente' | 'Pago' | 'Cancelado' | 'Vencido'

export interface CobrancaResponse {
  id: string
  customerName: string
  clienteWhatsapp: string
  contractId: string | null
  contratoTitulo: string | null
  reference: string
  amount: number
  dueDate: string
  paymentDate: string | null
  status: CobrancaStatus
  paymentMethod: string | null
  notes: string | null
  createdAt: string
  asaasId: string | null
  asaasPaymentLink: string | null
  asaasPixQrCode: string | null
  asaasBoletoUrl: string | null
}

export interface CobrancaListItem {
  id: string
  customerName: string
  contractId: string | null
  contratoTitulo: string | null
  reference: string
  amount: number
  dueDate: string
  status: CobrancaStatus
}

export interface CreateCobrancaRequest {
  customerId: string
  reference: string
  amount: number
  dueDate: string
  notes?: string
}

export interface PagarCobrancaRequest {
  paymentDate: string
  paymentMethod: string
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
