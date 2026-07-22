using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Automacao;

public class GeracaoCobrancaService(AppDbContext db)
{
    public async Task ProcessarTodosTenantsAsync(CancellationToken ct, DateOnly? hoje = null)
    {
        var dataHoje = hoje ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (dataHoje.Day != 1) return;

        var contratos = await db.Contracts
            .IgnoreQueryFilters()
            .Where(c => c.Status == ContratoStatus.Ativo)
            .ToListAsync(ct);

        if (contratos.Count == 0) return;

        var jaGerados = (await db.Charges
            .IgnoreQueryFilters()
            .Where(c => c.ContractId != null
                     && c.DueDate.Year  == dataHoje.Year
                     && c.DueDate.Month == dataHoje.Month)
            .Select(c => c.ContractId!.Value)
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var contrato in contratos)
            await ProcessarContratoAsync(contrato, dataHoje.Year, dataHoje.Month, jaGerados, ct);
    }

    private async Task ProcessarContratoAsync(
        Contract contrato, int ano, int mes,
        HashSet<Guid> jaGerados, CancellationToken ct)
    {
        if (jaGerados.Contains(contrato.Id)) return;

        var diaVenc = Math.Min(contrato.DueDay, DateTime.DaysInMonth(ano, mes));
        var dataVencimento = new DateOnly(ano, mes, diaVenc);

        var cobranca = new Charge
        {
            CompanyId      = contrato.CompanyId,
            CustomerId      = contrato.CustomerId,
            ContractId     = contrato.Id,
            Reference     = $"Mensalidade {mes:00}/{ano}",
            Amount          = contrato.Amount,
            DueDate = dataVencimento,
            Status         = CobrancaStatus.Pendente,
        };
        db.Charges.Add(cobranca);
        db.AutomationLogs.Add(new AutomationLog
        {
            CompanyId  = contrato.CompanyId,
            ChargeId = cobranca.Id,
            EventType = AutomacaoTipoEvento.CobrancaGerada,
            Success    = true,
        });
        await db.SaveChangesAsync(ct);
    }
}
