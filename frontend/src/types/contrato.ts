export type ContratoStatus = 'Rascunho' | 'Ativo' | 'Encerrado' | 'Cancelado'
export type TipoCobranca = 'Recorrente' | 'ParceladoPrazoFixo'
export type Periodicidade = 'Mensal' | 'Trimestral' | 'Semestral' | 'Anual'

export interface ContratoItemResponse {
  id: string
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface ContratoResponse {
  id: string
  numero: number
  clienteNome: string
  clienteWhatsapp: string
  titulo: string
  objeto: string
  tipoCobranca: TipoCobranca
  valor: number
  dataInicio: string
  dataFim: string | null
  periodicidade: Periodicidade
  diaVencimento: number
  status: ContratoStatus
  observacao: string | null
  criadoEm: string
  itens: ContratoItemResponse[]
  total: number
}

export interface ContratoListItem {
  id: string
  numero: number
  clienteNome: string
  titulo: string
  tipoCobranca: TipoCobranca
  valor: number
  status: ContratoStatus
  dataInicio: string
  dataFim: string | null
}

export interface ContratoItemRequest {
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface CreateContratoRequest {
  clienteId: string
  titulo: string
  objeto: string
  tipoCobranca: TipoCobranca
  valor: number
  dataInicio: string
  dataFim?: string
  periodicidade: Periodicidade
  diaVencimento: number
  observacao?: string
  itens: ContratoItemRequest[]
}

export interface GerarCobrancasRequest {
  de: string
  ate: string
}
