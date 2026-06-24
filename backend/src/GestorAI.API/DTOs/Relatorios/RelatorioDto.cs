namespace GestorAI.API.DTOs.Relatorios;

// Visão Geral
public record KpisGeralResponse(
    // Vendas (período)
    decimal Faturamento,
    decimal TicketMedio,
    decimal MargemEstimada,
    int TotalVendas,
    int ClientesAtendidos,
    // Financeiro (período)
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoPeriodo,
    decimal Inadimplencia,
    // Situação atual
    decimal ContasReceber,
    decimal TotalVencidoCobrancas,
    int ContratosAtivos,
    decimal MrrContratos,
    int CobrancasVencidas,
    int OrcamentosAbertos,
    int AgendamentosNoPeriodo,
    // Gráficos e tabelas
    List<TendenciaVendasResponse> TendenciaVendas,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<RankingProdutoResponse> TopProdutos,
    List<RankingClienteResponse> TopClientes);

// Vendas
public record TendenciaVendasResponse(DateTime Data, decimal Total, int Quantidade);
public record RankingProdutoResponse(string Nome, decimal Quantidade, decimal Total);
public record RankingClienteResponse(string Nome, int Compras, decimal Total);
public record VendasPorPagamentoResponse(string FormaPagamento, int Quantidade, decimal Total);

public record RelatorioVendasResponse(
    List<TendenciaVendasResponse> Tendencia,
    List<RankingProdutoResponse> TopProdutos,
    List<RankingClienteResponse> TopClientes,
    List<VendasPorPagamentoResponse> PorFormaPagamento);

// Financeiro
public record FluxoCaixaDiaResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);
public record CategoriaDespesaResponse(string Categoria, decimal Total);
public record LancamentoAnaliticoResponse(
    Guid Id,
    string Tipo,
    string Descricao,
    string Categoria,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status);

public record RelatorioFinanceiroResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Saldo,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<CategoriaDespesaResponse> CategoriasDespesas,
    List<LancamentoAnaliticoResponse> Analitico);

// Estoque
public record GiroProdutoResponse(string Nome, decimal Entradas, decimal Saidas, decimal GiroLiquido);
public record ProdutoSemMovimentacaoResponse(string Nome, decimal EstoqueAtual, decimal ValorEmEstoque);

public record RelatorioEstoqueResponse(
    decimal ValorTotalEstoque,
    int ProdutosAtivos,
    int ProdutosEstoqueBaixo,
    List<GiroProdutoResponse> GiroProdutos,
    List<ProdutoSemMovimentacaoResponse> SemMovimentacao);

// Clientes
public record ClienteRankingResponse(string Nome, string Whatsapp, int Compras, decimal TotalGasto);

public record RelatorioClientesResponse(
    int TotalClientes,
    int ClientesCompraram,
    decimal TicketMedioCliente,
    List<ClienteRankingResponse> TopClientes);

// Histórico de Clientes (lifetime)
public record HistoricoClienteItemResponse(
    Guid ClienteId,
    string Nome,
    string Whatsapp,
    int QtdPedidos,
    decimal TotalGasto,
    decimal TicketMedio,
    DateTime PrimeiraCompra,
    DateTime UltimaCompra,
    int? TempoMedioEntreComprasDias,
    int DiasDesdeUltimaCompra,
    string Classificacao);

public record HistoricoClientesResponse(
    int TotalClientesComCompras,
    int Recorrentes,
    int Inativos,
    int Novos,
    int EmRisco,
    decimal LtvMedio,
    decimal TicketMedioGeral,
    int? TempoMedioEntreComprasGeralDias,
    List<HistoricoClienteItemResponse> Clientes);

public record CompraHistoricoItemResponse(
    Guid VendaId,
    DateTime DataHora,
    int QtdItens,
    decimal Total,
    string FormaPagamento,
    string Status);

public record HistoricoClienteDetalheResponse(
    Guid ClienteId,
    string Nome,
    string Whatsapp,
    string? Email,
    DateTime DataCadastro,
    int QtdPedidos,
    decimal TotalGasto,
    decimal TicketMedio,
    DateTime PrimeiraCompra,
    DateTime UltimaCompra,
    int? TempoMedioEntreComprasDias,
    string Classificacao,
    List<CompraHistoricoItemResponse> Compras);

// Curva ABC
public record CurvaAbcItemResponse(
    string Nome,
    decimal Quantidade,
    decimal Total,
    decimal Percentual,
    decimal PercentualAcumulado,
    string Classe);

public record CurvaAbcResponse(
    List<CurvaAbcItemResponse> Itens,
    decimal TotalGeral);

// DRE
public record DreLinhaResponse(string Descricao, decimal Valor);

public record DreResponse(
    decimal ReceitaBrutaVendas,
    decimal OutrasReceitas,
    decimal TotalDescontos,
    decimal ReceitaLiquida,
    decimal Cmv,
    decimal LucroBruto,
    decimal MargemBruta,
    List<DreLinhaResponse> DespesasOperacionais,
    decimal TotalDespesasOperacionais,
    decimal ResultadoOperacional,
    decimal MargemOperacional);

// Agendamentos
public record RelatorioAgendamentosResponse(
    int TotalNoPeriodo,
    int Concluidos,
    int Cancelados,
    decimal TaxaConclusao,
    decimal TaxaOcupacao,
    List<AgendamentoStatusItemRel> PorStatus,
    List<AgendamentoProfissionalItemRel> PorProfissional);

public record AgendamentoStatusItemRel(string Status, int Count);
public record AgendamentoProfissionalItemRel(string Profissional, int Total, int Concluidos, decimal TaxaConclusao);

// Contratos
public record RelatorioContratosResponse(
    int TotalAtivos,
    decimal MrrTotal,
    int VencendoEm30,
    List<ContratoDetalheRel> Contratos);

public record ContratoDetalheRel(string Titulo, string ClienteNome, decimal Valor, string Periodicidade, DateOnly? DataFim, string Status);

// Cobranças
public record RelatorioCobrancasResponse(
    decimal TotalReceber,
    decimal TotalVencido,
    int VencidosCount,
    decimal TaxaInadimplencia,
    List<AgingFaixaRel> Aging,
    List<CobrancaDetalheRel> Cobrancas);

public record AgingFaixaRel(string Faixa, int Count, decimal Total);
public record CobrancaDetalheRel(string Referencia, string ClienteNome, decimal Valor, DateOnly DataVencimento, string Status, int DiasAtraso);

// Orçamentos
public record RelatorioOrcamentosResponse(
    int TotalNoPeriodo,
    decimal TaxaConversao,
    decimal ValorPipeline,
    List<OrcamentoStatusItemRel> PorStatus,
    List<OrcamentoDetalheRel> Orcamentos);

public record OrcamentoStatusItemRel(string Status, int Count, decimal ValorTotal);
public record OrcamentoDetalheRel(int Numero, string Titulo, string ClienteNome, decimal ValorTotal, string Status, DateTime CriadoEm);

// Assinaturas
public record RelatorioAssinaturasResponse(
    int TotalAtivas,
    decimal MrrTotal,
    int CanceladasNoPeriodo,
    decimal TaxaChurn,
    List<EvolucaoAssinaturaMesRel> Evolucao,
    List<AssinaturaDetalheRel> Assinaturas);

public record EvolucaoAssinaturaMesRel(string Mes, int Ativas, int Novas, int Canceladas);
public record AssinaturaDetalheRel(string ClienteNome, string Plano, decimal Valor, string Periodicidade, DateOnly DataInicio, DateOnly DataRenovacao, string Status);
