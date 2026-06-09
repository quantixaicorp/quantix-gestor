// backend/src/GestorAI.API/DTOs/Orcamentos/OrcamentoDto.cs
namespace GestorAI.API.DTOs.Orcamentos;

public record OrcamentoItemRequest(
    string Tipo,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record CreateOrcamentoRequest(
    Guid? ClienteId,
    string Titulo,
    DateTime DataValidade,
    string? Observacao,
    List<OrcamentoItemRequest> Itens);

public record OrcamentoItemResponse(
    Guid Id,
    string Tipo,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record OrcamentoResponse(
    Guid Id,
    int Numero,
    string Titulo,
    Guid? ClienteId,
    string? ClienteNome,
    string? ClienteWhatsapp,
    DateTime DataValidade,
    string Status,
    string? Observacao,
    Guid? VendaId,
    Guid? TokenPublico,
    DateTime CriadoEm,
    List<OrcamentoItemResponse> Itens,
    decimal Total);

public record OrcamentoListItem(
    Guid Id,
    int Numero,
    string Titulo,
    string? ClienteNome,
    DateTime DataValidade,
    string Status,
    decimal Total);

public record OrcamentoPublicoResponse(
    string Titulo,
    string? ClienteNome,
    DateTime DataValidade,
    string Status,
    string? Observacao,
    List<OrcamentoItemPublicoResponse> Itens,
    decimal Total);

public record OrcamentoItemPublicoResponse(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Total);
