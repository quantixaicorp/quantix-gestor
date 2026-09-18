using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Conciliacao;
using GestorAI.API.Services.Conciliacao;

namespace GestorAI.API.Endpoints;

public static class ConciliacaoEndpoints
{
    public static void MapConciliacao(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Bank Accounts
        group.MapGet("/bank-accounts", async (
            BankAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/bank-accounts", async (
            CreateBankAccountRequest req, BankAccountService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/bank-accounts/{result.Id}", result);
        });

        group.MapDelete("/bank-accounts/{id:guid}", async (
            Guid id, BankAccountService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Bank Statements
        group.MapPost("/bank-statements/import", async (
            HttpRequest httpReq,
            BankStatementParserService parser,
            BankReconciliationService svc,
            CancellationToken ct) =>
        {
            if (!httpReq.HasFormContentType)
                return Results.BadRequest("Envie um multipart/form-data.");

            var form = await httpReq.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("Campo 'file' obrigatório.");

            if (!Guid.TryParse(form["bankAccountId"], out var bankAccountId))
                return Results.BadRequest("Campo 'bankAccountId' inválido.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            BankStatementFormat format;
            List<ParsedTransaction> parsed;

            using var stream = file.OpenReadStream();
            if (ext == ".ofx")
            {
                format = BankStatementFormat.OFX;
                parsed = await parser.ParseOfxAsync(stream);
            }
            else if (ext == ".csv")
            {
                format = BankStatementFormat.CSV;
                parsed = await parser.ParseCsvAsync(stream);
            }
            else
            {
                return Results.BadRequest("Formato não suportado. Use .ofx ou .csv");
            }

            if (parsed.Count == 0)
                return Results.BadRequest("Nenhuma transação encontrada no arquivo.");

            var result = await svc.ImportAsync(bankAccountId, file.FileName, format, parsed, ct);
            return Results.Created($"/api/bank-statements/{result.Id}", result);
        });

        group.MapGet("/bank-statements", async (
            Guid? bankAccountId,
            BankReconciliationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListStatementsAsync(bankAccountId, ct)));

        group.MapGet("/bank-statements/{id:guid}/items", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetItemsAsync(id, ct)));

        group.MapDelete("/bank-statements/{id:guid}", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.DeleteStatementAsync(id, ct);
            return Results.NoContent();
        });

        // Reconciliation actions
        group.MapPost("/bank-reconciliation/match", async (
            ManualMatchRequest req, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.ManualMatchAsync(req, ct);
            return Results.Ok();
        });

        group.MapDelete("/bank-reconciliation/{id:guid}", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.UndoMatchAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/bank-reconciliation/{itemId:guid}/ignore", async (
            Guid itemId, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.IgnoreAsync(itemId, ct);
            return Results.Ok();
        });
    }
}
