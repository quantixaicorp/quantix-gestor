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
            var query = from l in db.AutomationLogs
                        join c in db.Charges on l.ChargeId equals c.Id
                        join cl in db.Customers on c.CustomerId equals cl.Id
                        select new { l, c, cl };

            if (apenasErros == true)
                query = query.Where(x => !x.l.Success);

            var logs = await query
                .OrderByDescending(x => x.l.CreatedAt)
                .Take(100)
                .Select(x => new AutomacaoLogResponse(
                    x.l.Id,
                    x.l.CreatedAt,
                    x.cl.Name,
                    x.c.Reference,
                    x.l.EventType,
                    x.l.Success,
                    x.l.ErrorMessage))
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
