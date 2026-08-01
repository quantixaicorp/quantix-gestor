using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new ContratoService(db, tenant));
    }

    private Customer CriarCliente(AppDbContext db)
    {
        var c = new Customer { CompanyId = _empresaId, Name = "João", WhatsApp = "11999990000" };
        db.Clientes.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_PersistsAsRascunho()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var req = new CreateContratoRequest(
            cliente.Id, "Plan Mensal", "Serviços mensais", "Recorrente",
            500m, DateOnly.FromDateTime(DateTime.Today), null,
            "Mensal", 10, null,
            [new ContratoItemRequest("Consulta", 1, 500m)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Rascunho", result.Status);
        Assert.Equal(1, result.Numero);
        Assert.Equal(500m, result.Total);
    }

    [Fact]
    public async Task AtivarAsync_RascunhoViradoAtivo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "T", Subject = "O",
            TipoCobranca = TipoCobranca.Recorrente, Amount = 100m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DueDay = 5,
            Status = ContratoStatus.Rascunho
        };
        contrato.Items.Add(new ContractItem { Description = "Serviço", Quantity = 1, UnitPrice = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var result = await svc.AtivarAsync(contrato.Id, default);

        Assert.Equal("IsActive", result.Status);
    }

    [Fact]
    public async Task AtivarAsync_SemItens_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "T", Subject = "O",
            TipoCobranca = TipoCobranca.Recorrente, Amount = 100m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DueDay = 5,
            Status = ContratoStatus.Rascunho
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.AtivarAsync(contrato.Id, default));
    }

    [Fact]
    public async Task GerarCobrancasAsync_CriaCobrancasNoPeriodo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "Mensal", Subject = "O",
            TipoCobranca = TipoCobranca.Recorrente, Amount = 200m,
            StartDate = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DueDay = 10,
            Status = ContratoStatus.IsActive
        };
        contrato.Items.Add(new ContractItem { Description = "S", Quantity = 1, UnitPrice = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        var result = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Equal(3, result.Count); // Jan, Fev, Mar
        Assert.All(result, c => Assert.Equal(200m, c.Amount));
        Assert.Equal(new DateOnly(2026, 1, 10), result[0].DueDate);
        Assert.Equal(new DateOnly(2026, 2, 10), result[1].DueDate);
        Assert.Equal(new DateOnly(2026, 3, 10), result[2].DueDate);
    }

    [Fact]
    public async Task GerarCobrancasAsync_NaoDuplica()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "Mensal", Subject = "O",
            TipoCobranca = TipoCobranca.Recorrente, Amount = 200m,
            StartDate = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DueDay = 10,
            Status = ContratoStatus.IsActive
        };
        contrato.Items.Add(new ContractItem { Description = "S", Quantity = 1, UnitPrice = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28));
        await svc.GerarCobrancasAsync(contrato.Id, req, default);
        var result2 = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Empty(result2); // already exist, no duplicates
        Assert.Equal(2, db.Charges.Count());
    }

    [Fact]
    public async Task GerarCobrancasAsync_ParceladoPrazoFixo_ValoresSomamTotal()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        // 3 parcelas de R$ 100,00 — cada uma R$ 33,33 exceto a última (R$ 33,34)
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "Parcelado", Subject = "O",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo, Amount = 100m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 3, 31),
            Periodicidade = Periodicidade.Mensal, DueDay = 10,
            Status = ContratoStatus.IsActive
        };
        contrato.Items.Add(new ContractItem { Description = "S", Quantity = 1, UnitPrice = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        var result = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Equal(3, result.Count);
        Assert.Equal(100m, result.Sum(c => c.Amount)); // total must equal contract value exactly
        Assert.Contains(result, c => c.Reference.StartsWith("Parcela 1/3"));
        Assert.Contains(result, c => c.Reference.StartsWith("Parcela 3/3"));
    }

    [Fact]
    public async Task AtivarAsync_ParceladoSemDataFim_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Numero = 1, Title = "T", Subject = "O",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo, Amount = 100m,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DueDay = 5,
            Status = ContratoStatus.Rascunho
        };
        contrato.Items.Add(new ContractItem { Description = "S", Quantity = 1, UnitPrice = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.AtivarAsync(contrato.Id, default));
    }
}
