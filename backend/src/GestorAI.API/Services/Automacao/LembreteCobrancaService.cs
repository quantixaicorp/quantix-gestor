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

        var configs = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .Where(c => c.EvolutionApiUrl != null
                     && c.EvolutionApiKey != null
                     && c.EvolutionInstance != null)
            .ToListAsync(ct);

        foreach (var config in configs)
            await ProcessarTenantAsync(config, hoje, ct);
    }

    private async Task ProcessarTenantAsync(ConfiguracaoEmpresa config, DateOnly hoje, CancellationToken ct)
    {
        var offsets = new List<(DateOnly TargetDate, AutomacaoTipoEvento TipoEvento)>();
        if (config.Lembrete3dAntes)  offsets.Add((hoje.AddDays(3),  AutomacaoTipoEvento.Lembrete3dAntes));
        if (config.Lembrete1dAntes)  offsets.Add((hoje.AddDays(1),  AutomacaoTipoEvento.Lembrete1dAntes));
        if (config.LembreteNoDia)    offsets.Add((hoje,             AutomacaoTipoEvento.LembreteNoDia));
        if (config.Lembrete1dDepois) offsets.Add((hoje.AddDays(-1), AutomacaoTipoEvento.Lembrete1dDepois));
        if (config.Lembrete3dDepois) offsets.Add((hoje.AddDays(-3), AutomacaoTipoEvento.Lembrete3dDepois));
        if (config.Lembrete7dDepois) offsets.Add((hoje.AddDays(-7), AutomacaoTipoEvento.Lembrete7dDepois));

        if (offsets.Count == 0) return;

        var eventosTipos = offsets.Select(o => (int)o.TipoEvento).ToList();
        var logsExistentes = (await db.AutomacaoLogs
            .IgnoreQueryFilters()
            .Where(l => l.EmpresaId == config.EmpresaId && eventosTipos.Contains((int)l.TipoEvento))
            .Select(l => new { l.CobrancaId, l.TipoEvento })
            .ToListAsync(ct))
            .Select(l => (l.CobrancaId, l.TipoEvento))
            .ToHashSet();

        var novosLogs = new List<AutomacaoLog>();

        foreach (var (targetDate, tipoEvento) in offsets)
        {
            var cobrancas = await db.Cobrancas
                .IgnoreQueryFilters()
                .Include(c => c.Cliente)
                .Where(c => c.EmpresaId == config.EmpresaId
                         && c.DataVencimento == targetDate
                         && c.Status == CobrancaStatus.Pendente)
                .ToListAsync(ct);

            foreach (var cobranca in cobrancas)
            {
                if (string.IsNullOrWhiteSpace(cobranca.Cliente?.Whatsapp))
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
                        cobranca.Cliente.Whatsapp, mensagem, ct);
                }
                catch (Exception ex)
                {
                    sucesso = false;
                    erroMsg = ex.Message;
                }

                novosLogs.Add(new AutomacaoLog
                {
                    EmpresaId  = config.EmpresaId,
                    CobrancaId = cobranca.Id,
                    TipoEvento = tipoEvento,
                    Sucesso    = sucesso,
                    ErroMsg    = erroMsg,
                });
                logsExistentes.Add((cobranca.Id, tipoEvento));
            }
        }

        if (novosLogs.Count > 0)
        {
            db.AutomacaoLogs.AddRange(novosLogs);
            await db.SaveChangesAsync(ct);
        }
    }

    private static string MontarMensagem(Cobranca cobranca, AutomacaoTipoEvento tipoEvento)
    {
        var nome  = cobranca.Cliente?.Nome ?? "Cliente";
        var ref_  = cobranca.Referencia;
        var valor = cobranca.Valor.ToString("N2");
        var venc  = cobranca.DataVencimento.ToString("dd/MM/yyyy");

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
