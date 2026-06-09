using GestorAI.API.Services.Orcamentos;

namespace GestorAI.API.Endpoints;

public static class OrcamentosPublicosEndpoints
{
    public static void MapOrcamentosPublicos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public/orcamentos").AllowAnonymous();

        group.MapGet("/{token:guid}", async (
            Guid token, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPublicoAsync(token, ct)));

        group.MapPost("/{token:guid}/aprovar", async (
            Guid token, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AprovarPublicoAsync(token, ct)));

        group.MapPost("/{token:guid}/rejeitar", async (
            Guid token, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.RejeitarPublicoAsync(token, ct)));
    }
}
