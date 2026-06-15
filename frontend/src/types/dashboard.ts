export type WidgetId =
  // Financeiro
  | 'kpi-saldo-mes'
  | 'kpi-receitas-mes'
  | 'kpi-despesas-mes'
  | 'kpi-contas-receber'
  | 'kpi-contas-vencidas'
  | 'kpi-contas-pagar-proximas'
  | 'kpi-inadimplencia'
  | 'kpi-saldo-projetado'
  | 'grafico-fluxo-caixa'
  | 'grafico-despesas-categoria'
  | 'grafico-receitas-categoria'
  | 'grafico-fluxo-anual'
  | 'tabela-contas-vencidas'
  | 'tabela-proximos-vencimentos'
  // Vendas
  | 'kpi-vendas-hoje'
  | 'kpi-vendas-mes'
  | 'kpi-qtd-vendas-mes'
  | 'kpi-ticket-medio'
  | 'kpi-lucro-estimado'
  | 'kpi-maior-venda-dia'
  | 'grafico-tendencia-vendas'
  | 'grafico-vendas-forma-pgto'
  | 'tabela-top-produtos'
  | 'tabela-ultimas-vendas'
  // Clientes
  | 'kpi-total-clientes'
  | 'kpi-clientes-novos-mes'
  | 'kpi-clientes-inativos'
  | 'tabela-top-clientes'
  // Estoque
  | 'kpi-valor-estoque'
  | 'kpi-produtos-ativos'
  | 'alerta-estoque-baixo'
  | 'grafico-distribuicao-categorias'
  | 'tabela-estoque-baixo'
  // Agendamentos
  | 'kpi-agendamentos-hoje'
  | 'kpi-agendamentos-confirmados'
  | 'kpi-agendamentos-cancelados'
  | 'kpi-taxa-conclusao-agendamentos'
  | 'kpi-taxa-ocupacao'
  | 'grafico-agenda-status'
  | 'grafico-ocupacao-profissional'
  | 'tabela-agenda-hoje'
  // Contratos
  | 'kpi-contratos-ativos'
  | 'kpi-mrr-contratos'
  | 'kpi-contratos-vencendo'
  | 'tabela-contratos-vencendo'
  // Cobranças
  | 'kpi-cobrancas-receber'
  | 'kpi-cobrancas-vencidas'
  | 'kpi-cobrancas-vencidas-count'
  | 'grafico-aging-cobrancas'
  | 'tabela-cobrancas-vencidas'
  // Orçamentos
  | 'kpi-orcamentos-abertos'
  | 'kpi-taxa-conversao-orcamentos'
  | 'kpi-pipeline'
  | 'grafico-orcamentos-status'
  // Assinaturas
  | 'kpi-assinaturas-ativas'
  | 'kpi-mrr-assinaturas'
  | 'kpi-churn-mes'
  | 'kpi-novas-assinaturas'
  | 'grafico-evolucao-assinaturas'

export interface DashboardLayoutDto {
  widgets: WidgetId[]
}

export interface KpiResponse {
  totalVendidoHoje: number
  totalVendidoMes: number
  lucroEstimadoMes: number
  contasPagarVencidas: number
  contasPagarProximas7Dias: number
  contasReceberPendentes: number
  produtosEstoqueBaixo: number
  totalReceitasMes: number
  totalDespesasMes: number
  qtyVendasMes: number
  ticketMedio: number
  totalClientes: number
  clientesNovosMes: number
  valorEstoque: number
  inadimplencia: number
  saldoProjetado: number
}

export interface VendasDiaResponse { data: string; total: number; quantidade: number }
export interface FluxoDiaResponse { data: string; receitas: number; despesas: number }
export interface TopProdutoResponse { nome: string; quantidadeVendida: number; totalFaturado: number }
export interface TopClienteResponse { nome: string; totalGasto: number }
export interface CategoriaDespesaDashResponse { categoria: string; total: number }

export interface DashboardResponse {
  kpis: KpiResponse
  vendasUltimos7Dias: VendasDiaResponse[]
  fluxoMes: FluxoDiaResponse[]
  topProdutos: TopProdutoResponse[]
  topClientes: TopClienteResponse[]
  despesasPorCategoria: CategoriaDespesaDashResponse[]
}

// ─── Dashboard Extras ───────────────────────────────────────────────────────
export interface UltimaVendaItem { id: string; dataHora: string; clienteNome: string; total: number; formaPagamento: string }
export interface VendaFormaPgtoItem { formaPagamento: string; quantidade: number; total: number }
export interface ReceitaCategoriaItem { categoria: string; total: number }
export interface FluxoMensalItem { mes: string; receitas: number; despesas: number; saldo: number }
export interface ContaVencidaDetalheItem { id: string; descricao: string; categoria: string; valor: number; dataVencimento: string; diasAtraso: number }
export interface ProximoVencimentoItem { id: string; descricao: string; categoria: string; valor: number; dataVencimento: string; diasParaVencer: number }
export interface EstoqueCategoriaItem { categoria: string; quantidade: number; valor: number }
export interface EstoqueBaixoDetalheItem { nome: string; estoqueAtual: number; estoqueMinimo: number; precoVenda: number }

export interface DashboardExtrasResponse {
  maiorVendaDia: number
  ultimasVendas: UltimaVendaItem[]
  vendasPorFormaPgto: VendaFormaPgtoItem[]
  receitasPorCategoria: ReceitaCategoriaItem[]
  fluxoAnual: FluxoMensalItem[]
  contasVencidas: ContaVencidaDetalheItem[]
  proximosVencimentos: ProximoVencimentoItem[]
  produtosAtivos: number
  distribuicaoCategorias: EstoqueCategoriaItem[]
  estoqueBaixo: EstoqueBaixoDetalheItem[]
  clientesInativos: number
}

// ─── Módulos (estendido) ────────────────────────────────────────────────────
export interface AgendamentoStatusItem { status: string; count: number }
export interface AgendamentoProfissionalItem { profissional: string; total: number; concluidos: number }
export interface AgendamentoDoDiaItem { clienteNome: string; servico: string; horaInicio: string; status: string }
export interface AgendamentosDashResponse {
  hoje: number
  confirmadosHoje: number
  canceladosMes: number
  taxaConclusaoMes: number
  taxaOcupacao: number
  porStatus: AgendamentoStatusItem[]
  porProfissional: AgendamentoProfissionalItem[]
  agendaHoje: AgendamentoDoDiaItem[]
}

export interface ContratoVencendoItem { titulo: string; clienteNome: string; valor: number; dataFim: string; diasRestantes: number }
export interface ContratosDashResponse {
  ativos: number; mrr: number; vencendoEm30: number
  contratosVencendo: ContratoVencendoItem[]
}

export interface CobrancaVencidaItem { referencia: string; clienteNome: string; valor: number; dataVencimento: string; diasAtraso: number }
export interface AgingFaixa { faixa: string; count: number; total: number }
export interface CobrancasDashResponse {
  totalReceber: number; vencidosCount: number; totalVencido: number
  cobrancasVencidas: CobrancaVencidaItem[]
  aging: AgingFaixa[]
}

export interface OrcamentoStatusItem { status: string; count: number; total: number }
export interface OrcamentosDashResponse {
  abertos: number; taxaConversao: number; valorPipeline: number
  porStatus: OrcamentoStatusItem[]
}

export interface EvolucaoAssinaturaMes { mes: string; ativas: number; novas: number; canceladas: number }
export interface AssinaturasDashResponse {
  ativas: number; mrr: number; canceladasMes: number; novasMes: number
  evolucao12Meses: EvolucaoAssinaturaMes[]
}

export interface ModulosDashboardResponse {
  agendamentos: AgendamentosDashResponse
  contratos: ContratosDashResponse
  cobrancas: CobrancasDashResponse
  orcamentos: OrcamentosDashResponse
  assinaturas: AssinaturasDashResponse
}
