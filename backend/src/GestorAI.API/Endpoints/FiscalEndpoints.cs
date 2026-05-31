using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.Services.Fiscal;

namespace GestorAI.API.Endpoints;

public static class FiscalEndpoints
{
    public static void MapFiscal(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/notas-fiscais", async (NotaFiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/notas-fiscais/emitir", async (
            EmitirNotaFiscalRequest req, NotaFiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.EmitirAsync(req, ct)));

        group.MapPost("/notas-fiscais/{id:guid}/cancelar", async (
            Guid id, CancelarNotaFiscalRequest req, NotaFiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, req, ct)));

        group.MapPost("/notas-fiscais/{id:guid}/consultar", async (
            Guid id, NotaFiscalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConsultarAsync(id, ct)));

        group.MapGet("/configuracao-empresa", async (ConfiguracaoEmpresaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ObterAsync(ct)));

        group.MapPut("/configuracao-empresa", async (
            AtualizarConfiguracaoEmpresaRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
            Results.Ok(await svc.AtualizarAsync(req, ct)));
    }
}
