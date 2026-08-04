export type ContratoStatus = 'Rascunho' | 'Ativo' | 'Encerrado' | 'Cancelado'
export type TipoCobranca = 'Recorrente' | 'ParceladoPrazoFixo'
export type Periodicidade = 'Mensal' | 'Trimestral' | 'Semestral' | 'Anual'

export interface ContratoItemResponse {
  id: string
  description: string
  quantity: number
  unitPrice: number
}

export interface ContratoResponse {
  id: string
  number: number
  customerName: string
  clienteWhatsapp: string
  title: string
  subject: string
  chargeType: TipoCobranca
  amount: number
  startDate: string
  endDate: string | null
  frequency: Periodicidade
  dueDay: number
  status: ContratoStatus
  notes: string | null
  createdAt: string
  items: ContratoItemResponse[]
  total: number
  clickSignStatus: string | null
  clickSignViewerUrl: string | null
}

export interface ContratoListItem {
  id: string
  number: number
  customerName: string
  title: string
  chargeType: TipoCobranca
  amount: number
  status: ContratoStatus
  startDate: string
  endDate: string | null
}

export interface ContratoItemRequest {
  description: string
  quantity: number
  unitPrice: number
}

export interface CreateContratoRequest {
  customerId: string
  title: string
  subject: string
  chargeType: TipoCobranca
  amount: number
  startDate: string
  endDate?: string
  frequency: Periodicidade
  dueDay: number
  notes?: string
  items: ContratoItemRequest[]
}

export interface GerarCobrancasRequest {
  de: string
  ate: string
}
