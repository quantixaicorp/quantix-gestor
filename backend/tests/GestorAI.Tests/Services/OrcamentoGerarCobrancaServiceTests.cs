using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Orcamentos;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class OrcamentoGerarCobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, OrcamentoService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new OrcamentoService(db, tenant));
    }

    private async Task<(Quote orc, Customer cliente)> CriarOrcamentoAsync(
        AppDbContext db, OrcamentoStatus status)
    {
        var cliente = new Customer { CompanyId = _empresaId, Name = "João", WhatsApp = "11988880000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var orc = new Quote
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 1,
            Title = "Desenvolvimento de Site",
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            Status = status,
        };
        orc.Items.Add(new QuoteItem
        {
            Type = OrcamentoItemTipo.Livre,
            Description = "Desenvolvimento",
            Quantity = 1,
            UnitPrice = 2500m,
        });
        db.Orcamentos.Add(orc);
        await db.SaveChangesAsync();
        return (orc, cliente);
    }

    [Fact]
    public async Task GerarCobrancaAsync_CriaCobrancaCorreta()
    {
        var (db, svc) = Setup();
        var (orc, cliente) = await CriarOrcamentoAsync(db, OrcamentoStatus.Aprovado);
        var vencimento = new DateOnly(2026, 8, 1);

        var cobranca = await svc.GerarCobrancaAsync(orc.Id, vencimento, default);

        Assert.Equal(2500m, cobranca.Amount);
        Assert.Equal(vencimento, cobranca.DueDate);
        Assert.Contains("ORC-001", cobranca.Reference);
        Assert.Equal("Pendente", cobranca.Status);
    }

    [Fact]
    public async Task GerarCobrancaAsync_LancaExcecao_SeRascunho()
    {
        var (db, svc) = Setup();
        var (orc, _) = await CriarOrcamentoAsync(db, OrcamentoStatus.Rascunho);

        await Assert.ThrowsAsync<GestorAI.API.Shared.Exceptions.AppException>(
            () => svc.GerarCobrancaAsync(orc.Id, new DateOnly(2026, 8, 1), default));
    }

    [Fact]
    public async Task GerarCobrancaAsync_NaoAlteraStatusOrcamento()
    {
        var (db, svc) = Setup();
        var (orc, _) = await CriarOrcamentoAsync(db, OrcamentoStatus.Aprovado);

        await svc.GerarCobrancaAsync(orc.Id, new DateOnly(2026, 8, 1), default);

        var orcAtualizado = await db.Orcamentos.FindAsync(orc.Id);
        Assert.Equal(OrcamentoStatus.Aprovado, orcAtualizado!.Status);
    }
}
