using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class EstoqueEndpoints
{
    public static void MapEstoque(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Categorias
        group.MapGet("/categorias", async (CategoriaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/categorias", async (CreateCategoriaRequest req, CategoriaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/categorias/{result.Id}", result);
        });

        // Produtos
        group.MapGet("/produtos", async (
            string? busca, Guid? categoriaId, bool? estoqueBaixo,
            ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, categoriaId, estoqueBaixo, ct)));

        group.MapGet("/produtos/{id:guid}", async (Guid id, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/produtos", async (CreateProdutoRequest req, ProdutoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/produtos/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateProdutoRequest>>();

        group.MapPut("/produtos/{id:guid}", async (
            Guid id, UpdateProdutoRequest req, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/produtos/{id:guid}", async (
            Guid id, ProdutoService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Estoque
        group.MapPost("/estoque/movimentar", async (
            EntradaEstoqueRequest req, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EntradaEstoqueAsync(req, ct)))
            .AddEndpointFilter<ValidationFilter<EntradaEstoqueRequest>>();

        group.MapGet("/estoque/movimentacoes", async (
            Guid? produtoId, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListMovimentacoesAsync(produtoId, ct)));
    }
}
