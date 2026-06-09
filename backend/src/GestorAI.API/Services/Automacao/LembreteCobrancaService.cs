using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Automacao;

public class LembreteCobrancaService(AppDbContext db, IEvolutionApiService evolutionService)
{
    public async Task ProcessarTodosTenantsAsync(CancellationToken ct, DateOnly? hoje = null)
    {
        var dataHoje = hoje ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var configs = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .Where(c => c.EvolutionApiUrl != null && c.EvolutionApiKey != null && c.EvolutionInstance != null)
            .ToListAsync(ct);

        foreach (var config in configs)
            await ProcessarTenantAsync(config, dataHoje, ct);
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

                var jaEnviou = await db.AutomacaoLogs
                    .IgnoreQueryFilters()
                    .AnyAsync(l => l.CobrancaId == cobranca.Id && l.TipoEvento == tipoEvento, ct);
                if (jaEnviou) continue;

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

                db.AutomacaoLogs.Add(new AutomacaoLog
                {
                    EmpresaId = config.EmpresaId,
                    CobrancaId = cobranca.Id,
                    TipoEvento = tipoEvento,
                    Sucesso = sucesso,
                    ErroMsg = erroMsg,
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static string MontarMensagem(Cobranca cobranca, AutomacaoTipoEvento tipoEvento)
    {
        var nome = cobranca.Cliente!.Nome;
        var valor = cobranca.Valor.ToString("F2");
        var ref_ = cobranca.Referencia;
        var data = cobranca.DataVencimento.ToString("dd/MM/yyyy");

        return tipoEvento switch
        {
            AutomacaoTipoEvento.Lembrete1dDepois or
            AutomacaoTipoEvento.Lembrete3dDepois or
            AutomacaoTipoEvento.Lembrete7dDepois =>
                $"Olá {nome}, sua cobrança de R$ {valor} ({ref_}) venceu em {data} e ainda está em aberto. Por favor, regularize.",
            _ =>
                $"Olá {nome}, sua cobrança de R$ {valor} ({ref_}) vence em {data}. Por favor, realize o pagamento para evitar juros.",
        };
    }
}
