using Microsoft.Extensions.Hosting;

namespace GestorAI.API.Services.Automacao;

public class AutomacaoHostedService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private DateTime _ultimaExecucao = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var agora = DateTime.UtcNow;
            if (agora.Hour == 7 && agora.Date > _ultimaExecucao.Date)
            {
                _ultimaExecucao = agora;
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var lembretes = scope.ServiceProvider.GetRequiredService<LembreteCobrancaService>();
                    var geracao   = scope.ServiceProvider.GetRequiredService<GeracaoCobrancaService>();
                    await lembretes.ProcessarTodosTenantsAsync(ct);
                    await geracao.ProcessarTodosTenantsAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // log and continue so the background loop doesn't die permanently
                    _ = ex;
                }
            }
        }
    }
}
