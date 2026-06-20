export type RelatorioTabId =
  | 'visao-geral'
  | 'vendas'
  | 'financeiro'
  | 'estoque'
  | 'clientes'
  | 'agendamentos'
  | 'contratos'
  | 'cobrancas'
  | 'orcamentos'
  | 'assinaturas'
  | 'curva-abc'
  | 'dre'
  | 'compras'

export interface RelatorioLayoutDto {
  tabs: RelatorioTabId[]
}

export interface KpisGeralResponse {
  // Vendas (período)
  faturamento: number
  ticketMedio: number
  margemEstimada: number
  totalVendas: number
  clientesAtendidos: number
  // Financeiro (período)
  totalReceitas: number
  totalDespesas: number
  saldoPeriodo: number
  inadimplencia: number
  // Situação atual
  contasReceber: number
  totalVencidoCobrancas: number
  contratosAtivos: number
  mrrContratos: number
  cobrancasVencidas: number
  orcamentosAbertos: number
  agendamentosNoPeriodo: number
  // Gráficos e tabelas
  tendenciaVendas: TendenciaVendasResponse[]
  fluxoPorDia: FluxoCaixaDiaResponse[]
  topProdutos: RankingProdutoResponse[]
  topClientes: RankingClienteResponse[]
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
export interface LancamentoAnaliticoResponse {
  id: string; tipo: string; descricao: string; categoria: string; valor: number
  dataVencimento: string; dataPagamento: string | null; status: string
}
export interface RelatorioFinanceiroResponse {
  totalReceitas: number
  totalDespesas: number
  saldo: number
  fluxoPorDia: FluxoCaixaDiaResponse[]
  categoriasDespesas: CategoriaDespesaResponse[]
  analitico: LancamentoAnaliticoResponse[]
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

// Curva ABC
export interface CurvaAbcItemResponse {
  nome: string
  quantidade: number
  total: number
  percentual: number
  percentualAcumulado: number
  classe: 'A' | 'B' | 'C'
}
export interface CurvaAbcResponse {
  itens: CurvaAbcItemResponse[]
  totalGeral: number
}

// DRE
export interface DreLinhaResponse { descricao: string; valor: number }
export interface DreResponse {
  receitaBrutaVendas: number
  outrasReceitas: number
  totalDescontos: number
  receitaLiquida: number
  cmv: number
  lucroBruto: number
  margemBruta: number
  despesasOperacionais: DreLinhaResponse[]
  totalDespesasOperacionais: number
  resultadoOperacional: number
  margemOperacional: number
}

// ─── Agendamentos ────────────────────────────────────────────────────────────
export interface AgendamentoStatusItemRel { status: string; count: number }
export interface AgendamentoProfissionalItemRel { profissional: string; total: number; concluidos: number; taxaConclusao: number }
export interface RelatorioAgendamentosResponse {
  totalNoPeriodo: number
  concluidos: number
  cancelados: number
  taxaConclusao: number
  taxaOcupacao: number
  porStatus: AgendamentoStatusItemRel[]
  porProfissional: AgendamentoProfissionalItemRel[]
}

// ─── Contratos ───────────────────────────────────────────────────────────────
export interface ContratoDetalheRel { titulo: string; clienteNome: string; valor: number; periodicidade: string; dataFim: string | null; status: string }
export interface RelatorioContratosResponse {
  totalAtivos: number
  mrrTotal: number
  vencendoEm30: number
  contratos: ContratoDetalheRel[]
}

// ─── Cobranças ───────────────────────────────────────────────────────────────
export interface AgingFaixaRel { faixa: string; count: number; total: number }
export interface CobrancaDetalheRel { referencia: string; clienteNome: string; valor: number; dataVencimento: string; status: string; diasAtraso: number }
export interface RelatorioCobrancasResponse {
  totalReceber: number
  totalVencido: number
  vencidosCount: number
  taxaInadimplencia: number
  aging: AgingFaixaRel[]
  cobrancas: CobrancaDetalheRel[]
}

// ─── Orçamentos ──────────────────────────────────────────────────────────────
export interface OrcamentoStatusItemRel { status: string; count: number; valorTotal: number }
export interface OrcamentoDetalheRel { numero: number; titulo: string; clienteNome: string; valorTotal: number; status: string; criadoEm: string }
export interface RelatorioOrcamentosResponse {
  totalNoPeriodo: number
  taxaConversao: number
  valorPipeline: number
  porStatus: OrcamentoStatusItemRel[]
  orcamentos: OrcamentoDetalheRel[]
}

// ─── Assinaturas ─────────────────────────────────────────────────────────────
export interface EvolucaoAssinaturaMesRel { mes: string; ativas: number; novas: number; canceladas: number }
export interface AssinaturaDetalheRel { clienteNome: string; plano: string; valor: number; periodicidade: string; dataInicio: string; dataRenovacao: string; status: string }
export interface RelatorioAssinaturasResponse {
  totalAtivas: number
  mrrTotal: number
  canceladasNoPeriodo: number
  taxaChurn: number
  evolucao: EvolucaoAssinaturaMesRel[]
  assinaturas: AssinaturaDetalheRel[]
}
