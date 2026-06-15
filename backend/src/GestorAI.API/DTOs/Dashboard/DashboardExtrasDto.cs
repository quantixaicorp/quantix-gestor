namespace GestorAI.API.DTOs.Dashboard;

// Vendas extras
public record UltimaVendaResponse(Guid Id, DateTime DataHora, string ClienteNome, decimal Total, string FormaPagamento);
public record VendaFormaPgtoResponse(string FormaPagamento, int Quantidade, decimal Total);

// Financeiro extras
public record ReceitaCategoriaResponse(string Categoria, decimal Total);
public record FluxoMensalResponse(string Mes, decimal Receitas, decimal Despesas, decimal Saldo);
public record ContaVencidaDetalheResponse(Guid Id, string Descricao, string Categoria, decimal Valor, DateTime DataVencimento, int DiasAtraso);
public record ProximoVencimentoResponse(Guid Id, string Descricao, string Categoria, decimal Valor, DateTime DataVencimento, int DiasParaVencer);

// Estoque extras
public record EstoqueCategoriaResponse(string Categoria, int Quantidade, decimal Valor);
public record EstoqueBaixoDetalheResponse(string Nome, decimal EstoqueAtual, decimal EstoqueMinimo, decimal PrecoVenda);

// Resposta agregada
public record DashboardExtrasResponse(
    decimal MaiorVendaDia,
    List<UltimaVendaResponse> UltimasVendas,
    List<VendaFormaPgtoResponse> VendasPorFormaPgto,
    List<ReceitaCategoriaResponse> ReceitasPorCategoria,
    List<FluxoMensalResponse> FluxoAnual,
    List<ContaVencidaDetalheResponse> ContasVencidas,
    List<ProximoVencimentoResponse> ProximosVencimentos,
    int ProdutosAtivos,
    List<EstoqueCategoriaResponse> DistribuicaoCategorias,
    List<EstoqueBaixoDetalheResponse> EstoqueBaixo,
    int ClientesInativos
);
