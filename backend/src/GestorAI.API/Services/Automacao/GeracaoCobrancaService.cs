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

        var contratos = await db.Contratos
            .IgnoreQueryFilters()
            .Where(c => c.Status == ContratoStatus.Ativo)
            .ToListAsync(ct);

        if (contratos.Count == 0) return;

        var jaGerados = (await db.Cobrancas
            .IgnoreQueryFilters()
            .Where(c => c.ContratoId != null
                     && c.DataVencimento.Year  == dataHoje.Year
                     && c.DataVencimento.Month == dataHoje.Month)
            .Select(c => c.ContratoId!.Value)
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var contrato in contratos)
            await ProcessarContratoAsync(contrato, dataHoje.Year, dataHoje.Month, jaGerados, ct);
    }

    private async Task ProcessarContratoAsync(
        Contrato contrato, int ano, int mes,
        HashSet<Guid> jaGerados, CancellationToken ct)
    {
        if (jaGerados.Contains(contrato.Id)) return;

        var diaVenc = Math.Min(contrato.DiaVencimento, DateTime.DaysInMonth(ano, mes));
        var dataVencimento = new DateOnly(ano, mes, diaVenc);

        var cobranca = new Cobranca
        {
            EmpresaId      = contrato.EmpresaId,
            ClienteId      = contrato.ClienteId,
            ContratoId     = contrato.Id,
            Referencia     = $"Mensalidade {mes:00}/{ano}",
            Valor          = contrato.Valor,
            DataVencimento = dataVencimento,
            Status         = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        db.AutomacaoLogs.Add(new AutomacaoLog
        {
            EmpresaId  = contrato.EmpresaId,
            CobrancaId = cobranca.Id,
            TipoEvento = AutomacaoTipoEvento.CobrancaGerada,
            Sucesso    = true,
        });
        await db.SaveChangesAsync(ct);
    }
}
