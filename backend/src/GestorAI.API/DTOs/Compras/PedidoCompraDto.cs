namespace GestorAI.API.DTOs.Compras;

public record ItemPedidoRequest(
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorEstimado);

public record ItemPedidoResponse(
    Guid Id,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorEstimado);

public record CreatePedidoCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? Observacoes,
    List<ItemPedidoRequest> Itens);

public record UpdatePedidoCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? Observacoes,
    List<ItemPedidoRequest> Itens);

public record PedidoCompraResponse(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid FornecedorId,
    string FornecedorNome,
    string Status,
    decimal ValorEstimado,
    string? Observacoes,
    DateTime CriadoEm,
    List<ItemPedidoResponse> Itens);
