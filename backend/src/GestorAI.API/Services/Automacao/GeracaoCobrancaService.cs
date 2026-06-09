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

        foreach (var contrato in contratos)
            await ProcessarContratoAsync(contrato, dataHoje.Year, dataHoje.Month, ct);
    }

    private async Task ProcessarContratoAsync(Contrato contrato, int ano, int mes, CancellationToken ct)
    {
        var jaExiste = await db.Cobrancas
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ContratoId == contrato.Id
                        && c.DataVencimento.Year == ano
                        && c.DataVencimento.Month == mes, ct);
        if (jaExiste) return;

        var diaVenc = Math.Min(contrato.DiaVencimento, DateTime.DaysInMonth(ano, mes));
        var dataVencimento = new DateOnly(ano, mes, diaVenc);

        var cobranca = new Cobranca
        {
            EmpresaId = contrato.EmpresaId,
            ClienteId = contrato.ClienteId,
            ContratoId = contrato.Id,
            Referencia = $"Mensalidade {mes:00}/{ano}",
            Valor = contrato.Valor,
            DataVencimento = dataVencimento,
            Status = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);

        db.AutomacaoLogs.Add(new AutomacaoLog
        {
            EmpresaId = contrato.EmpresaId,
            CobrancaId = cobranca.Id,
            TipoEvento = AutomacaoTipoEvento.CobrancaGerada,
            Sucesso = true,
        });
        await db.SaveChangesAsync(ct);
    }
}
