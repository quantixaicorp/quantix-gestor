export interface OrcamentoItemPublico {
  descricao: string
  quantidade: number
  valorUnitario: number
  total: number
}

export interface OrcamentoPublico {
  titulo: string
  clienteNome: string | null
  dataValidade: string
  status: string
  observacao: string | null
  itens: OrcamentoItemPublico[]
  total: number
}
