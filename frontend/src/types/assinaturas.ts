// frontend/src/types/assinaturas.ts
export interface PlanoItem {
  id: string
  descricao: string
  servicoId: string | null
  quantidadePorCiclo: number
  tipo: 'Servico' | 'Desconto' | 'Beneficio'
  percentualDesconto: number | null
}

export interface PlanoAssinaturaListItem {
  id: string
  nome: string
  nicho: string
  preco: number
  periodicidade: string
  ativo: boolean
  maisVendido: boolean
  totalAssinantes: number
}

export interface PlanoAssinaturaResponse extends PlanoAssinaturaListItem {
  descricao: string | null
  itens: PlanoItem[]
  criadoEm: string
}

export interface NichoTemplateItem {
  id: string
  descricao: string
  quantidadePorCiclo: number
  tipo: string
  percentualDesconto: number | null
}

export interface NichoTemplate {
  id: string
  nicho: string
  nomePlano: string
  descricao: string | null
  precoSugerido: number
  maisVendido: boolean
  periodicidade: string
  itens: NichoTemplateItem[]
}

export interface AssinaturaListItem {
  id: string
  clienteNome: string
  planNome: string
  status: string
  dataRenovacao: string
  cicloAtual: number
}
