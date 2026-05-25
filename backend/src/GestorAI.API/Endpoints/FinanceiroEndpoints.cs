using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class FinanceiroEndpoints
{
    public static void MapFinanceiro(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization("FinanceAccess");

        group.MapGet("/lancamentos", async (
            string? tipo, string? status, DateTime? vencimentoAte,
            LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(tipo, status, vencimentoAte, ct)));

        group.MapGet("/lancamentos/{id:guid}", async (
            Guid id, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/lancamentos", async (
            CreateLancamentoRequest req, LancamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/lancamentos/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateLancamentoRequest>>();

        group.MapPost("/lancamentos/{id:guid}/pagar", async (
            Guid id, PagarLancamentoRequest req, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.PagarAsync(id, req, ct)));

        group.MapPost("/lancamentos/{id:guid}/cancelar", async (
            Guid id, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapGet("/financeiro/fluxo-caixa", async (
            DateTime de, DateTime ate, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetFluxoCaixaAsync(de, ate, ct)));
    }
}
