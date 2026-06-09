using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CobrancaResumoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
    }

    private Cobranca MakeCobranca(DateOnly vencimento, CobrancaStatus status,
        DateTime? dataPagamento = null) => new()
    {
        EmpresaId = _empresaId,
        ClienteId = Guid.NewGuid(),
        Referencia = "REF",
        Valor = 100m,
        DataVencimento = vencimento,
        Status = status,
        DataPagamento = dataPagamento,
    };

    [Fact]
    public async Task GetResumoAsync_ContabilizaTotaisCorretamente()
    {
        var db = CreateDb();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Pendente com vencimento futuro → TotalAReceber
        db.Cobrancas.Add(MakeCobranca(hoje.AddDays(5), CobrancaStatus.Pendente));
        // Pendente com vencimento passado → TotalVencido
        db.Cobrancas.Add(MakeCobranca(hoje.AddDays(-3), CobrancaStatus.Pendente));
        // Pago no mês atual → TotalRecebido
        db.Cobrancas.Add(MakeCobranca(hoje, CobrancaStatus.Pago, inicioMes.AddDays(2)));
        // Cancelado — não entra em nada
        db.Cobrancas.Add(MakeCobranca(hoje, CobrancaStatus.Cancelado));
        await db.SaveChangesAsync();

        var svc = new CobrancaService(db, new TenantContext { EmpresaId = _empresaId }, null!);
        var resumo = await svc.GetResumoAsync(default);

        Assert.Equal(100m, resumo.TotalAReceber);
        Assert.Equal(100m, resumo.TotalVencido);
        Assert.Equal(100m, resumo.TotalRecebido);
    }
}
