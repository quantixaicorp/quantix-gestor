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
  | 'historico-clientes'

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

export interface TendenciaVendasResponse { data: string; total: number; quantity: number }
export interface RankingProdutoResponse { name: string; quantity: number; total: number }
export interface RankingClienteResponse { name: string; compras: number; total: number }
export interface VendasPorPagamentoResponse { paymentMethod: string; quantity: number; total: number }
export interface RelatorioVendasResponse {
  tendencia: TendenciaVendasResponse[]
  topProdutos: RankingProdutoResponse[]
  topClientes: RankingClienteResponse[]
  porFormaPagamento: VendasPorPagamentoResponse[]
}

export interface FluxoCaixaDiaResponse { data: string; receitas: number; despesas: number; saldo: number }
export interface CategoriaDespesaResponse { category: string; total: number }
export interface LancamentoAnaliticoResponse {
  id: string; type: string; description: string; category: string; amount: number
  dueDate: string; paymentDate: string | null; status: string
}
export interface RelatorioFinanceiroResponse {
  totalReceitas: number
  totalDespesas: number
  saldo: number
  fluxoPorDia: FluxoCaixaDiaResponse[]
  categoriasDespesas: CategoriaDespesaResponse[]
  analitico: LancamentoAnaliticoResponse[]
}

export interface GiroProdutoResponse { name: string; entradas: number; saidas: number; giroLiquido: number }
export interface ProdutoSemMovimentacaoResponse { name: string; currentStock: number; valorEmEstoque: number }
export interface RelatorioEstoqueResponse {
  valorTotalEstoque: number
  produtosAtivos: number
  produtosEstoqueBaixo: number
  giroProdutos: GiroProdutoResponse[]
  semMovimentacao: ProdutoSemMovimentacaoResponse[]
}

export interface ClienteRankingResponse { name: string; whatsApp: string; compras: number; totalGasto: number }
export interface RelatorioClientesResponse {
  totalClientes: number
  clientesCompraram: number
  ticketMedioCliente: number
  topClientes: ClienteRankingResponse[]
}

// Histórico de Clientes (lifetime)
export interface HistoricoClienteItemResponse {
  customerId: string
  name: string
  whatsApp: string
  qtdPedidos: number
  totalGasto: number
  ticketMedio: number
  primeiraCompra: string
  ultimaCompra: string
  tempoMedioEntreComprasDias: number | null
  diasDesdeUltimaCompra: number
  classificacao: string
}
export interface HistoricoClientesResponse {
  totalClientesComCompras: number
  recorrentes: number
  inativos: number
  novos: number
  emRisco: number
  ltvMedio: number
  ticketMedioGeral: number
  tempoMedioEntreComprasGeralDias: number | null
  clientes: HistoricoClienteItemResponse[]
}
export interface CompraHistoricoItemResponse {
  saleId: string
  saleDate: string
  qtdItens: number
  total: number
  paymentMethod: string
  status: string
}
export interface HistoricoClienteDetalheResponse {
  customerId: string
  name: string
  whatsApp: string
  email: string | null
  createdAt: string
  qtdPedidos: number
  totalGasto: number
  ticketMedio: number
  primeiraCompra: string
  ultimaCompra: string
  tempoMedioEntreComprasDias: number | null
  classificacao: string
  compras: CompraHistoricoItemResponse[]
}

// Curva ABC
export interface CurvaAbcItemResponse {
  name: string
  quantity: number
  total: number
  percentual: number
  percentualAcumulado: number
  classe: 'A' | 'B' | 'C'
}
export interface CurvaAbcResponse {
  items: CurvaAbcItemResponse[]
  totalGeral: number
}

// DRE
export interface DreLinhaResponse { description: string; amount: number }
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
export interface AgendamentoProfissionalItemRel { professional: string; total: number; concluidos: number; taxaConclusao: number }
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
export interface ContratoDetalheRel { title: string; customerName: string; amount: number; frequency: string; endDate: string | null; status: string }
export interface RelatorioContratosResponse {
  totalAtivos: number
  mrrTotal: number
  vencendoEm30: number
  contratos: ContratoDetalheRel[]
}

// ─── Cobranças ───────────────────────────────────────────────────────────────
export interface AgingFaixaRel { faixa: string; count: number; total: number }
export interface CobrancaDetalheRel { reference: string; customerName: string; amount: number; dueDate: string; status: string; diasAtraso: number }
export interface RelatorioCobrancasResponse {
  totalReceber: number
  totalVencido: number
  vencidosCount: number
  taxaInadimplencia: number
  aging: AgingFaixaRel[]
  charges: CobrancaDetalheRel[]
}

// ─── Orçamentos ──────────────────────────────────────────────────────────────
export interface OrcamentoStatusItemRel { status: string; count: number; totalAmount: number }
export interface OrcamentoDetalheRel { number: number; title: string; customerName: string; totalAmount: number; status: string; createdAt: string }
export interface RelatorioOrcamentosResponse {
  totalNoPeriodo: number
  taxaConversao: number
  valorPipeline: number
  porStatus: OrcamentoStatusItemRel[]
  orcamentos: OrcamentoDetalheRel[]
}

// ─── Assinaturas ─────────────────────────────────────────────────────────────
export interface EvolucaoAssinaturaMesRel { mes: string; ativas: number; novas: number; canceladas: number }
export interface AssinaturaDetalheRel { customerName: string; plan: string; amount: number; frequency: string; startDate: string; renewalDate: string; status: string }
export interface RelatorioAssinaturasResponse {
  totalAtivas: number
  mrrTotal: number
  canceladasNoPeriodo: number
  taxaChurn: number
  evolucao: EvolucaoAssinaturaMesRel[]
  assinaturas: AssinaturaDetalheRel[]
}
