using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoResumoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new LancamentoService(db, tc));
    }

    [Fact]
    public async Task GetResumoAsync_ContabilizaTotaisCorretamente()
    {
        var (db, svc) = Setup();
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Receita paga no mês → TotalReceitasMes
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
            Descricao = "Receita", Valor = 500m,
            DataVencimento = hoje, Status = StatusLancamento.Pago,
            DataPagamento = inicioMes.AddDays(2), Categoria = "Serviço",
        });
        // Despesa paga no mês → TotalDespesasMes
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Despesa", Valor = 200m,
            DataVencimento = hoje, Status = StatusLancamento.Pago,
            DataPagamento = inicioMes.AddDays(1), Categoria = "Aluguel",
        });
        // Pendente com vencimento futuro → TotalPendente
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "A pagar", Valor = 100m,
            DataVencimento = hoje.AddDays(5), Status = StatusLancamento.Pendente,
            Categoria = "Fornecedor",
        });
        // Cancelado — não entra em nada
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
            Descricao = "Cancelado", Valor = 999m,
            DataVencimento = hoje, Status = StatusLancamento.Cancelado,
            Categoria = "Outros",
        });
        await db.SaveChangesAsync();

        var resumo = await svc.GetResumoAsync(default);

        Assert.Equal(500m, resumo.TotalReceitasMes);
        Assert.Equal(200m, resumo.TotalDespesasMes);
        Assert.Equal(300m, resumo.SaldoMes);
        Assert.Equal(100m, resumo.TotalPendente);
    }
}
