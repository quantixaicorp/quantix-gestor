using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoResumoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService svc) Setup()
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        var parcelamentoService = new ParcelamentoService(db, tc);
        return (db, new LancamentoService(db, tc, parcelamentoService));
    }

    [Fact]
    public async Task GetResumoAsync_ContabilizaTotaisCorretamente()
    {
        var (db, svc) = Setup();
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Receita paga no mês → TotalReceitasMes
        db.Lancamentos.Add(new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Receita,
            Description = "Receita", Amount = 500m,
            DueDate = hoje, Status = StatusLancamento.Pago,
            PaymentDate = inicioMes.AddDays(2), Category = "Serviço",
        });
        // Despesa paga no mês → TotalDespesasMes
        db.Lancamentos.Add(new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Despesa,
            Description = "Despesa", Amount = 200m,
            DueDate = hoje, Status = StatusLancamento.Pago,
            PaymentDate = inicioMes.AddDays(1), Category = "Aluguel",
        });
        // Pendente com vencimento futuro → TotalPendente
        db.Lancamentos.Add(new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Despesa,
            Description = "A pagar", Amount = 100m,
            DueDate = hoje.AddDays(5), Status = StatusLancamento.Pendente,
            Category = "Supplier",
        });
        // Cancelado — não entra em nada
        db.Lancamentos.Add(new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Receita,
            Description = "Cancelado", Amount = 999m,
            DueDate = hoje, Status = StatusLancamento.Cancelado,
            Category = "Outros",
        });
        await db.SaveChangesAsync();

        var resumo = await svc.GetResumoAsync(default);

        Assert.Equal(500m, resumo.TotalReceitasMes);
        Assert.Equal(200m, resumo.TotalDespesasMes);
        Assert.Equal(300m, resumo.SaldoMes);
        Assert.Equal(100m, resumo.TotalPendente);
    }
}
