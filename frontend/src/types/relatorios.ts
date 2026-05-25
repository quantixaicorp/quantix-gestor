export interface KpisGeralResponse {
  faturamento: number
  ticketMedio: number
  margemEstimada: number
  inadimplencia: number
  totalVendas: number
}

export interface TendenciaVendasResponse { data: string; total: number; quantidade: number }
export interface RankingProdutoResponse { nome: string; quantidade: number; total: number }
export interface RankingClienteResponse { nome: string; compras: number; total: number }
export interface VendasPorPagamentoResponse { formaPagamento: string; quantidade: number; total: number }
export interface RelatorioVendasResponse {
  tendencia: TendenciaVendasResponse[]
  topProdutos: RankingProdutoResponse[]
  topClientes: RankingClienteResponse[]
  porFormaPagamento: VendasPorPagamentoResponse[]
}

export interface FluxoCaixaDiaResponse { data: string; receitas: number; despesas: number; saldo: number }
export interface CategoriaDespesaResponse { categoria: string; total: number }
export interface RelatorioFinanceiroResponse {
  totalReceitas: number
  totalDespesas: number
  saldo: number
  fluxoPorDia: FluxoCaixaDiaResponse[]
  categoriasDespesas: CategoriaDespesaResponse[]
}

export interface GiroProdutoResponse { nome: string; entradas: number; saidas: number; giroLiquido: number }
export interface ProdutoSemMovimentacaoResponse { nome: string; estoqueAtual: number; valorEmEstoque: number }
export interface RelatorioEstoqueResponse {
  valorTotalEstoque: number
  produtosAtivos: number
  produtosEstoqueBaixo: number
  giroProdutos: GiroProdutoResponse[]
  semMovimentacao: ProdutoSemMovimentacaoResponse[]
}

export interface ClienteRankingResponse { nome: string; whatsapp: string; compras: number; totalGasto: number }
export interface RelatorioClientesResponse {
  totalClientes: number
  clientesCompraram: number
  ticketMedioCliente: number
  topClientes: ClienteRankingResponse[]
}
