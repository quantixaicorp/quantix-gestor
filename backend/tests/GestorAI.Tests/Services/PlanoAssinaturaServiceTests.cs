using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Assinaturas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class PlanoAssinaturaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, PlanoAssinaturaService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new PlanoAssinaturaService(db, tenant));
    }

    [Fact]
    public async Task CreateAsync_PersistsPlanoWithItens()
    {
        var (db, svc) = Setup();
        var req = new CreatePlanoAssinaturaRequest(
            "Básico", "Plan simples", "Barbearia", 79m, "Mensal", false,
            [new PlanoItemRequest("Corte", null, 2, "Servico", null)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Básico", result.Name);
        Assert.Equal(79m, result.Price);
        Assert.Single(result.Items);
        Assert.Equal("Corte", result.Items[0].Description);
    }

    [Fact]
    public async Task CreateAsync_PeriodicidadeInvalida_LancaExcecao()
    {
        var (_, svc) = Setup();
        var req = new CreatePlanoAssinaturaRequest("X", null, "X", 99m, "Invalido", false, []);

        await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(req, default));
    }

    [Fact]
    public async Task UpdateAsync_AlteraAtivo()
    {
        var (db, svc) = Setup();
        var plano = new SubscriptionPlan
        {
            CompanyId = _empresaId, Name = "Original", Price = 50m,
            Frequency = Periodicidade.Mensal
        };
        db.SubscriptionPlans.Add(plano);
        await db.SaveChangesAsync();

        var req = new UpdatePlanoAssinaturaRequest("Atualizado", null, "X", 60m, "Mensal", false, false, []);
        var result = await svc.UpdateAsync(plano.Id, req, default);

        Assert.Equal("Atualizado", result.Name);
        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_PlanoComAssinantes_LancaExcecao()
    {
        var (db, svc) = Setup();
        var plano = new SubscriptionPlan { CompanyId = _empresaId, Name = "P", Price = 99m, Frequency = Periodicidade.Mensal };
        db.SubscriptionPlans.Add(plano);
        var cliente = new Customer { CompanyId = _empresaId, Name = "C", WhatsApp = "11999990000" };
        db.Customers.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId, CustomerId = cliente.Id, Title = "T", Subject = "O",
            ChargeType = TipoCobranca.Recorrente, Amount = 99m, Number = 1,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            Frequency = Periodicidade.Mensal, DueDay = 1, Status = ContratoStatus.Ativo
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();
        db.CustomerSubscriptions.Add(new CustomerSubscription
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            SubscriptionPlanId = plano.Id, ContractId = contrato.Id,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            RenewalDate = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(plano.Id, default));
    }
}
