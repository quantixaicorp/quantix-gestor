using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoRenovarServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new ContratoService(db, tenant));
    }

    private async Task<Contract> CriarContratoAsync(AppDbContext db, ContratoStatus status, DateOnly? dataFim = null)
    {
        var cliente = new Customer { CompanyId = _empresaId, Name = "Ana", WhatsApp = "11999990000" };
        db.Clientes.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 1,
            Title = "Serviço Mensal",
            Subject = "Prestação",
            TipoCobranca = TipoCobranca.Recorrente,
            Amount = 500m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = dataFim,
            Periodicidade = Periodicidade.Mensal,
            DueDay = 10,
            Status = status,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();
        return contrato;
    }

    [Fact]
    public async Task RenovarAsync_CriaNovoContratoRascunho()
    {
        var (db, svc) = Setup();
        var dataFim = new DateOnly(2026, 12, 31);
        var original = await CriarContratoAsync(db, ContratoStatus.IsActive, dataFim);

        var novo = await svc.RenovarAsync(original.Id, default);

        Assert.NotEqual(original.Id, novo.Id);
        Assert.Equal("Rascunho", novo.Status);
        Assert.Equal(new DateOnly(2027, 1, 1), novo.StartDate);
        Assert.Null(novo.EndDate);
        Assert.Equal(original.Title, novo.Title);
        Assert.Equal(original.Amount, novo.Amount);
    }

    [Fact]
    public async Task RenovarAsync_LancaExcecao_SeNaoAtivo()
    {
        var (db, svc) = Setup();
        var contrato = await CriarContratoAsync(db, ContratoStatus.Encerrado, new DateOnly(2026, 12, 31));

        await Assert.ThrowsAsync<GestorAI.API.Shared.Exceptions.AppException>(
            () => svc.RenovarAsync(contrato.Id, default));
    }

    [Fact]
    public async Task ListVencendoAsync_RetornaContratosNoPrazo()
    {
        var (db, svc) = Setup();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        await CriarContratoAsync(db, ContratoStatus.IsActive, hoje.AddDays(15));  // dentro do prazo
        await CriarContratoAsync(db, ContratoStatus.IsActive, hoje.AddDays(60));  // fora do prazo
        await CriarContratoAsync(db, ContratoStatus.IsActive, null);              // sem EndDate

        var vencendo = await svc.ListVencendoAsync(30, default);

        Assert.Single(vencendo);
        Assert.Equal(hoje.AddDays(15), vencendo[0].EndDate);
    }

    [Fact]
    public async Task ListVencendoAsync_ExcluiContratosForaDoPrazo()
    {
        var (db, svc) = Setup();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        await CriarContratoAsync(db, ContratoStatus.IsActive, hoje.AddDays(60));  // fora do prazo de 30 dias
        await CriarContratoAsync(db, ContratoStatus.IsActive, null);              // sem EndDate — deve ser excluído

        var vencendo = await svc.ListVencendoAsync(30, default);

        Assert.Empty(vencendo);
    }
}
