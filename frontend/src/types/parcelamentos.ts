export type StatusParcelamento = 'EmAberto' | 'PagoParcialmente' | 'PagoTotal' | 'Cancelado'

export interface ParcelaResponse {
  id: string
  numeroParcela: number
  valor: number
  dataVencimento: string
  dataPagamento?: string
  status: string
  vencido: boolean
}

export interface ParcelamentoResponse {
  id: string
  compraId?: string
  descricao: string
  valorTotal: number
  qtdParcelas: number
  status: StatusParcelamento
  categoria: string
  parcelas: ParcelaResponse[]
}
