using System.Security.Cryptography;
using System.Text;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Endpoints;

public static class AdminEndpoints
{
    private static readonly string[] Tables =
    [
        "Produtos", "Categorias", "MovimentacoesEstoque", "Vendas", "ItensVenda",
        "Clientes", "Lancamentos", "Orcamentos", "OrcamentoItens", "Profissionais",
        "DisponibilidadeSemanais", "BloqueiosAgenda", "Agendamentos", "NotasFiscais",
        "NotaFiscalItens", "ConfiguracoesEmpresa", "Contratos", "Cobrancas", "Fornecedores",
    ];

    public static void MapAdmin(this IEndpointRouteBuilder app)
    {
        // One-time data migration: assigns all Guid.Empty tenant records to the given companyId.
        // Protected by a secret key. Remove this endpoint after running once in production.
        app.MapPost("/admin/fix-tenant", async (
            HttpContext ctx,
            string companyId,
            AppDbContext db,
            IConfiguration config,
            CancellationToken ct) =>
        {
            // Fail-closed: sem segredo configurado (via variável de ambiente
            // AdminFixKey), o endpoint fica inerte. A chave é recebida no
            // cabeçalho X-Admin-Key para não vazar em logs/histórico de URL.
            var secret = config["AdminFixKey"];
            var key = ctx.Request.Headers["X-Admin-Key"].ToString();
            if (string.IsNullOrWhiteSpace(secret)
                || !CryptographicOperations.FixedTimeEquals(
                       Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(secret)))
                return Results.Unauthorized();

            if (!Guid.TryParse(companyId, out var id))
                return Results.BadRequest(new { error = "companyId inválido" });

            var updated = new Dictionary<string, int>();

            foreach (var table in Tables)
            {
                // table is from a hardcoded constant array — no injection risk
#pragma warning disable EF1002
                var rows = await db.Database.ExecuteSqlRawAsync(
                    $"""UPDATE "{table}" SET "EmpresaId" = @p0 WHERE "EmpresaId" = @p1""",
                    id, Guid.Empty);
#pragma warning restore EF1002
                if (rows > 0) updated[table] = rows;
            }

            return Results.Ok(new { migrated = updated, companyId = id });
        }).AllowAnonymous();
    }
}
