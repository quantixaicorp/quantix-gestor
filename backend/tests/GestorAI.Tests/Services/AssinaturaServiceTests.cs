using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Assinaturas;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestorAI.Tests.Services;

public class AssinaturaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, TenantContext tenant, AssinaturaService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = _empresaId,
            Slug = "minha-empresa",
            AsaasApiKey = "test_key",
            AsaasSandbox = true,
        });
        db.SaveChanges();

        var httpFactory = new FakeHttpClientFactory();
        var asaasService = new AsaasService(httpFactory);
        var cobrancaService = new CobrancaService(db, tenant, asaasService);
        var svc = new AssinaturaService(db, tenant, cobrancaService);
        return (db, tenant, svc);
    }

    private SubscriptionPlan CriarPlano(AppDbContext db)
    {
        var plano = new SubscriptionPlan
        {
            CompanyId = _empresaId, Name = "Premium", Price = 129m,
            Frequency = Periodicidade.Mensal, Niche = "Barbearia"
        };
        plano.Items.Add(new SubscriptionPlanItem { Description = "Corte", QuantityPerCycle = 4, Type = TipoItemPlano.Servico });
        db.SubscriptionPlans.Add(plano);
        db.SaveChanges();
        return plano;
    }

    [Fact]
    public async Task AssinarAsync_CriaClienteContratoCobranca()
    {
        var (db, tenant, svc) = Setup();
        var plano = CriarPlano(db);

        var req = new AssinarRequest("João Silva", "11999990000", "joao@test.com");
        var assinatura = await svc.AssinarSemAsaasAsync(_empresaId, plano.Id, req, default);

        Assert.Equal(_empresaId, tenant.CompanyId);

        var clienteExiste = await db.Customers.IgnoreQueryFilters()
            .AnyAsync(c => c.WhatsApp == "11999990000");
        Assert.True(clienteExiste);

        var contratoExiste = await db.Contracts.IgnoreQueryFilters()
            .AnyAsync(c => c.CustomerSubscriptionId == assinatura.AssinaturaId);
        Assert.True(contratoExiste);

        var cobrancaExiste = await db.Charges.IgnoreQueryFilters()
            .AnyAsync(c => c.ContractId == assinatura.ContractId);
        Assert.True(cobrancaExiste);
    }

    [Fact]
    public async Task AssinarAsync_ClienteJaExiste_ReusaCliente()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        db.Customers.Add(new Customer { CompanyId = _empresaId, Name = "João", WhatsApp = "11999990000" });
        await db.SaveChangesAsync();

        var req = new AssinarRequest("João Silva", "11999990000", null);
        await svc.AssinarSemAsaasAsync(_empresaId, plano.Id, req, default);

        var count = await db.Customers.IgnoreQueryFilters()
            .CountAsync(c => c.WhatsApp == "11999990000");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CancelarAsync_MarcaCanceladaEEncerraContrato()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        var cliente = new Customer { CompanyId = _empresaId, Name = "Maria", WhatsApp = "11888880000" };
        db.Customers.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id, Title = "T", Subject = "O",
            ChargeType = TipoCobranca.Recorrente, Amount = 129m, Number = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Frequency = Periodicidade.Mensal, DueDay = 1, Status = ContratoStatus.Ativo
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();

        var assinatura = new CustomerSubscription
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            SubscriptionPlanId = plano.Id, ContractId = contrato.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            RenewalDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };
        db.CustomerSubscriptions.Add(assinatura);
        await db.SaveChangesAsync();

        await svc.CancelarAsync(assinatura.Id, default);

        var a = await db.CustomerSubscriptions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == assinatura.Id);
        Assert.Equal(AssinaturaStatus.Cancelada, a!.Status);

        var c = await db.Contracts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, c!.Status);
    }
}

public class FakeHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
