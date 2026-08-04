export type TipoProduto = 'Produto' | 'Servico'

export interface CategoriaResponse { id: string; name: string }

export interface ProdutoResponse {
  id: string
  categoryId: string
  categoriaNome: string
  name: string
  description: string | null
  salePrice: number
  averageCost: number
  currentStock: number
  minimumStock: number
  barcode: string | null
  isActive: boolean
  estoqueBaixo: boolean
  durationMinutes: number | null
  type: TipoProduto
}

export interface CreateProdutoRequest {
  categoryId: string
  name: string
  description?: string
  salePrice: number
  averageCost: number
  currentStock: number
  minimumStock: number
  barcode?: string
  type: TipoProduto
  durationMinutes?: number | null
}

export interface UpdateProdutoRequest {
  categoryId: string
  name: string
  description?: string
  salePrice: number
  minimumStock: number
  barcode?: string
  isActive: boolean
  durationMinutes?: number | null
}

export interface EntradaEstoqueRequest {
  productId: string
  quantity: number
  custoUnitario?: number
  notes?: string
}

export interface MovimentacaoResponse {
  id: string
  productId: string
  produtoNome: string
  type: string
  quantity: number
  source: string
  movementDate: string
  notes: string | null
}
