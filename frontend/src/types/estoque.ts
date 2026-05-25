export interface CategoriaResponse { id: string; nome: string }

export interface ProdutoResponse {
  id: string
  categoriaId: string
  categoriaNome: string
  nome: string
  descricao: string | null
  precoVenda: number
  custoMedio: number
  estoqueAtual: number
  estoqueMinimo: number
  codigoBarras: string | null
  ativo: boolean
  estoqueBaixo: boolean
}

export interface CreateProdutoRequest {
  categoriaId: string
  nome: string
  descricao?: string
  precoVenda: number
  custoMedio: number
  estoqueAtual: number
  estoqueMinimo: number
  codigoBarras?: string
}

export interface UpdateProdutoRequest {
  categoriaId: string
  nome: string
  descricao?: string
  precoVenda: number
  estoqueMinimo: number
  codigoBarras?: string
  ativo: boolean
}

export interface EntradaEstoqueRequest {
  produtoId: string
  quantidade: number
  custoUnitario?: number
  observacao?: string
}

export interface MovimentacaoResponse {
  id: string
  produtoId: string
  produtoNome: string
  tipo: string
  quantidade: number
  origem: string
  dataHora: string
  observacao: string | null
}
