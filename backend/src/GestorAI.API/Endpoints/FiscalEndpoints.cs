using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.DTOs.PublicBooking;
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

        group.MapPut("/configuracao-empresa/branding", async (
            ConfigurarBrandingRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
            Results.Ok(await svc.SalvarBrandingAsync(req, ct)));

        group.MapPost("/configuracao-empresa/logo", async (
            HttpContext ctx, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            var file = ctx.Request.Form.Files.GetFile("file");
            if (file is null) return Results.BadRequest(new { error = "Arquivo não encontrado" });
            return Results.Ok(new { logoUrl = await svc.UploadLogoAsync(file, ct) });
        });

        group.MapPut("/configuracao-empresa/integracoes", async (
            SalvarIntegracoesRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarIntegracoesAsync(req, ct);
            return Results.Ok();
        });

        group.MapPut("/configuracao-empresa/automacao", async (
            SalvarAutomacaoConfigRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarAutomacaoConfigAsync(req, ct);
            return Results.Ok();
        });

        group.MapPut("/configuracao-empresa/white-label", async (
            SalvarWhiteLabelRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarWhiteLabelAsync(req, ct);
            return Results.Ok();
        });

        group.MapPut("/configuracao-empresa/agendamento", async (
            SalvarAgendamentoConfigRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarAgendamentoAsync(req, ct);
            return Results.Ok();
        });
    }
}
