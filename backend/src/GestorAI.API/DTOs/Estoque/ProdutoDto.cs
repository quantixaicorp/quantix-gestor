namespace GestorAI.API.DTOs.Estoque;

public record ProdutoResponse(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal CustoMedio,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    string? CodigoBarras,
    bool Ativo,
    bool EstoqueBaixo,
    int? DuracaoMinutos);

public record CreateProdutoRequest(
    Guid CategoriaId,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal CustoMedio,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    string? CodigoBarras);

public record UpdateProdutoRequest(
    Guid CategoriaId,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal EstoqueMinimo,
    string? CodigoBarras,
    bool Ativo,
    int? DuracaoMinutos);

public record EntradaEstoqueRequest(
    Guid ProdutoId,
    decimal Quantidade,
    decimal? CustoUnitario,
    string? Observacao);
