export type StatusParcelamento = 'EmAberto' | 'PagoParcialmente' | 'PagoTotal' | 'Cancelado'

export interface ParcelaResponse {
  id: string
  installmentNumber: number
  amount: number
  dueDate: string
  paymentDate?: string
  status: string
  vencido: boolean
}

export interface ParcelamentoResponse {
  id: string
  purchaseId?: string
  description: string
  totalAmount: number
  installmentCount: number
  status: StatusParcelamento
  category: string
  installments: ParcelaResponse[]
}
