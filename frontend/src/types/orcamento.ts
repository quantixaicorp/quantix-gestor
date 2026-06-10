export interface OrcamentoItemRequest {
  tipo: 'Produto' | 'Livre'
  produtoId?: string
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface CreateOrcamentoRequest {
  clienteId?: string
  titulo: string
  dataValidade: string
  observacao?: string
  itens: OrcamentoItemRequest[]
}

export interface OrcamentoItemResponse {
  id: string
  tipo: 'Produto' | 'Livre'
  produtoId: string | null
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface OrcamentoResponse {
  id: string
  numero: number
  titulo: string
  clienteId: string | null
  clienteNome: string | null
  clienteWhatsapp: string | null
  dataValidade: string
  status: OrcamentoStatus
  observacao: string | null
  vendaId: string | null
  tokenPublico: string | null
  criadoEm: string
  itens: OrcamentoItemResponse[]
  total: number
}

export interface OrcamentoListItem {
  id: string
  numero: number
  titulo: string
  clienteNome: string | null
  dataValidade: string
  status: OrcamentoStatus
  total: number
}

export type OrcamentoStatus =
  | 'Rascunho'
  | 'Enviado'
  | 'Aprovado'
  | 'Convertido'
  | 'Rejeitado'
  | 'Cancelado'
  | 'Expirado'
