namespace GestorAI.API.DTOs.Relatorios;

// Visão Geral
public record KpisGeralResponse(
    decimal Faturamento,
    decimal TicketMedio,
    decimal MargemEstimada,
    decimal Inadimplencia,
    int TotalVendas);

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

public record RelatorioFinanceiroResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal Saldo,
    List<FluxoCaixaDiaResponse> FluxoPorDia,
    List<CategoriaDespesaResponse> CategoriasDespesas);

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
