export interface KpiResponse {
  totalVendidoHoje: number
  totalVendidoMes: number
  lucroEstimadoMes: number
  contasPagarVencidas: number
  contasPagarProximas7Dias: number
  contasReceberPendentes: number
  produtosEstoqueBaixo: number
}

export interface VendasDiaResponse { data: string; total: number; quantidade: number }
export interface FluxoDiaResponse { data: string; receitas: number; despesas: number }
export interface TopProdutoResponse { nome: string; quantidadeVendida: number; totalFaturado: number }

export interface DashboardResponse {
  kpis: KpiResponse
  vendasUltimos7Dias: VendasDiaResponse[]
  fluxoMes: FluxoDiaResponse[]
  topProdutos: TopProdutoResponse[]
}
