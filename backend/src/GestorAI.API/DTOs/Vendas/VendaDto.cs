namespace GestorAI.API.DTOs.Vendas;

public record ItemVendaRequest(Guid ProdutoId, decimal Quantidade, decimal Desconto);

public record CreateVendaRequest(
    Guid? ClienteId,
    List<ItemVendaRequest> Itens,
    decimal Desconto,
    string FormaPagamento,
    int? Parcelas,
    string? Observacao,
    DateTime? DataHora = null,
    Guid? ProfissionalId = null,
    string? ObservacaoOS = null);

public record ItemVendaResponse(
    Guid ProdutoId,
    string ProdutoNome,
    decimal Quantidade,
    decimal PrecoUnitario,
    decimal Desconto,
    decimal Total);

public record VendaResponse(
    Guid Id,
    Guid? ClienteId,
    string? ClienteNome,
    DateTime DataHora,
    string Status,
    decimal Subtotal,
    decimal Desconto,
    decimal Total,
    string FormaPagamento,
    int? Parcelas,
    string? Observacao,
    List<ItemVendaResponse> Itens,
    string? ProfissionalNome = null,
    string? ObservacaoOS = null);

public record VendaListItem(
    Guid Id,
    Guid? ClienteId,
    string? ClienteNome,
    DateTime DataHora,
    string Status,
    decimal Total,
    string FormaPagamento,
    string? ProfissionalNome = null);

public record FecharVendaRequest(string FormaPagamento, int? Parcelas, string? Observacao);

public record UpdateVendaRequest(
    Guid? ClienteId,
    string FormaPagamento,
    DateTime DataHora);
