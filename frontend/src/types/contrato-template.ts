export interface ContratoTemplateItem {
  id: string
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface ContratoTemplate {
  id: string
  nome: string
  objeto: string
  tipoCobranca: string
  periodicidade: string
  diaVencimento: number
  valorPadrao: number | null
  criadoEm: string
  itens: ContratoTemplateItem[]
  total: number
}

export interface ContratoTemplateListItem {
  id: string
  nome: string
  tipoCobranca: string
  periodicidade: string
  valorPadrao: number | null
  qtdItens: number
}

export interface CreateContratoTemplateRequest {
  nome: string
  objeto: string
  tipoCobranca: string
  periodicidade: string
  diaVencimento: number
  valorPadrao: number | null
  itens: { descricao: string; quantidade: number; valorUnitario: number }[]
}
