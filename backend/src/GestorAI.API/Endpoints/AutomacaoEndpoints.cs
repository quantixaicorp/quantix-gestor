using GestorAI.API.DTOs.Automacao;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Automacao;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Endpoints;

public static class AutomacaoEndpoints
{
    public static void MapAutomacao(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/automacao").RequireAuthorization();

        group.MapGet("/log", async (
            AppDbContext db, CancellationToken ct,
            bool? apenasErros = null) =>
        {
            var query = from l in db.AutomacaoLogs
                        join c in db.Cobrancas on l.CobrancaId equals c.Id
                        join cl in db.Clientes on c.ClienteId equals cl.Id
                        select new { l, c, cl };

            if (apenasErros == true)
                query = query.Where(x => !x.l.Sucesso);

            var logs = await query
                .OrderByDescending(x => x.l.CriadoEm)
                .Take(100)
                .Select(x => new AutomacaoLogResponse(
                    x.l.Id,
                    x.l.CriadoEm,
                    x.cl.Nome,
                    x.c.Referencia,
                    x.l.TipoEvento,
                    x.l.Sucesso,
                    x.l.ErroMsg))
                .ToListAsync(ct);

            return Results.Ok(logs);
        });

        group.MapPost("/testar-conexao", async (
            TestarConexaoRequest req, IEvolutionApiService evolutionSvc, CancellationToken ct) =>
        {
            var ok = await evolutionSvc.TestarConexaoAsync(req.ApiUrl, req.ApiKey, ct);
            return Results.Ok(new { sucesso = ok });
        });
    }
}
