using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Automacao;

public class LembreteCobrancaService(AppDbContext db, IEvolutionApiService evolutionService)
{
    public async Task ProcessarTodosTenantsAsync(CancellationToken ct, DateOnly? hojeOverride = null)
    {
        var hoje = hojeOverride ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var configs = await db.CompanySettings
            .IgnoreQueryFilters()
            .Where(c => c.EvolutionApiUrl != null
                     && c.EvolutionApiKey != null
                     && c.EvolutionInstance != null)
            .ToListAsync(ct);

        foreach (var config in configs)
            await ProcessarTenantAsync(config, hoje, ct);
    }

    private async Task ProcessarTenantAsync(CompanySettings config, DateOnly hoje, CancellationToken ct)
    {
        var offsets = new List<(DateOnly TargetDate, AutomacaoTipoEvento EventType)>();
        if (config.Reminder3DaysBefore)  offsets.Add((hoje.AddDays(3),  AutomacaoTipoEvento.Lembrete3dAntes));
        if (config.Reminder1DayBefore)  offsets.Add((hoje.AddDays(1),  AutomacaoTipoEvento.Lembrete1dAntes));
        if (config.ReminderOnDueDate)    offsets.Add((hoje,             AutomacaoTipoEvento.LembreteNoDia));
        if (config.Reminder1DayAfter) offsets.Add((hoje.AddDays(-1), AutomacaoTipoEvento.Lembrete1dDepois));
        if (config.Reminder3DaysAfter) offsets.Add((hoje.AddDays(-3), AutomacaoTipoEvento.Lembrete3dDepois));
        if (config.Reminder7DaysAfter) offsets.Add((hoje.AddDays(-7), AutomacaoTipoEvento.Lembrete7dDepois));

        if (offsets.Count == 0) return;

        var eventosTipos = offsets.Select(o => (int)o.EventType).ToList();
        var logsExistentes = (await db.AutomationLogs
            .IgnoreQueryFilters()
            .Where(l => l.CompanyId == config.CompanyId && eventosTipos.Contains((int)l.EventType))
            .Select(l => new { l.ChargeId, l.EventType })
            .ToListAsync(ct))
            .Select(l => (l.ChargeId, l.EventType))
            .ToHashSet();

        var novosLogs = new List<AutomationLog>();

        foreach (var (targetDate, tipoEvento) in offsets)
        {
            var cobrancas = await db.Charges
                .IgnoreQueryFilters()
                .Include(c => c.Customer)
                .Where(c => c.CompanyId == config.CompanyId
                         && c.DueDate == targetDate
                         && c.Status == CobrancaStatus.Pendente)
                .ToListAsync(ct);

            foreach (var cobranca in cobrancas)
            {
                if (string.IsNullOrWhiteSpace(cobranca.Customer?.WhatsApp))
                    continue;

                if (logsExistentes.Contains((cobranca.Id, tipoEvento)))
                    continue;

                var mensagem = MontarMensagem(cobranca, tipoEvento);
                bool sucesso;
                string? erroMsg = null;
                try
                {
                    sucesso = await evolutionService.EnviarMensagemAsync(
                        config.EvolutionApiUrl!, config.EvolutionApiKey!, config.EvolutionInstance!,
                        cobranca.Customer.WhatsApp, mensagem, ct);
                }
                catch (Exception ex)
                {
                    sucesso = false;
                    erroMsg = ex.Message;
                }

                novosLogs.Add(new AutomationLog
                {
                    CompanyId  = config.CompanyId,
                    ChargeId = cobranca.Id,
                    EventType = tipoEvento,
                    Success    = sucesso,
                    ErrorMessage    = erroMsg,
                });
                logsExistentes.Add((cobranca.Id, tipoEvento));
            }
        }

        if (novosLogs.Count > 0)
        {
            db.AutomationLogs.AddRange(novosLogs);
            await db.SaveChangesAsync(ct);
        }
    }

    private static string MontarMensagem(Charge cobranca, AutomacaoTipoEvento tipoEvento)
    {
        var nome  = cobranca.Customer?.Name ?? "Customer";
        var ref_  = cobranca.Reference;
        var valor = cobranca.Amount.ToString("N2");
        var venc  = cobranca.DueDate.ToString("dd/MM/yyyy");

        return tipoEvento switch
        {
            AutomacaoTipoEvento.Lembrete3dAntes  => $"Olá {nome}, lembramos que a cobrança de {ref_} no valor de R$ {valor} vence em 3 dias ({venc}).",
            AutomacaoTipoEvento.Lembrete1dAntes  => $"Olá {nome}, sua cobrança de {ref_} no valor de R$ {valor} vence amanhã ({venc}).",
            AutomacaoTipoEvento.LembreteNoDia    => $"Olá {nome}, sua cobrança de {ref_} no valor de R$ {valor} vence hoje ({venc}).",
            AutomacaoTipoEvento.Lembrete1dDepois => $"Olá {nome}, sua cobrança de {ref_} no valor de R$ {valor} venceu ontem ({venc}). Por favor, regularize.",
            AutomacaoTipoEvento.Lembrete3dDepois => $"Olá {nome}, sua cobrança de {ref_} no valor de R$ {valor} está em atraso há 3 dias ({venc}). Por favor, regularize.",
            AutomacaoTipoEvento.Lembrete7dDepois => $"Olá {nome}, sua cobrança de {ref_} no valor de R$ {valor} está em atraso há 7 dias ({venc}). Entre em contato.",
            _                                     => $"Olá {nome}, você possui uma cobrança de {ref_} no valor de R$ {valor} com vencimento em {venc}.",
        };
    }
}
