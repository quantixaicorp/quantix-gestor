using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CobrancaService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new CobrancaService(db, tenant, null!));
    }

    private Customer CriarCliente(AppDbContext db)
    {
        var c = new Customer { CompanyId = _empresaId, Name = "Ana", WhatsApp = "11988880000" };
        db.Customers.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_PersistsAsPendente()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var req = new CreateCobrancaRequest(
            cliente.Id, "Mensalidade Jun/2026", 300m,
            new DateOnly(2026, 6, 10), null);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Pendente", result.Status);
        Assert.Equal(300m, result.Amount);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPago()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Charge
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Reference = "Test", Amount = 100m,
            DueDate = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Pendente
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        var req = new PagarCobrancaRequest(DateTime.UtcNow, "Pix");
        var result = await svc.PagarAsync(cobranca.Id, req, default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal("Pix", result.PaymentMethod);
        Assert.NotNull(result.PaymentDate);
    }

    [Fact]
    public async Task PagarAsync_QuandoCancelado_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Charge
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Reference = "Test", Amount = 100m,
            DueDate = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Cancelado
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.PagarAsync(cobranca.Id, new PagarCobrancaRequest(DateTime.UtcNow, "Pix"), default));
    }

    [Fact]
    public async Task GetWhatsappUrlAsync_RetornaUrlCorreta()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db); // WhatsApp = "11988880000"
        var cobranca = new Charge
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Reference = "Mensalidade Jun/2026", Amount = 300m,
            DueDate = new DateOnly(2026, 6, 10),
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        var result = await svc.GetWhatsappUrlAsync(cobranca.Id, default);

        Assert.StartsWith("https://wa.me/5511988880000", result.Url);
        Assert.Contains("Mensalidade+Jun%2F2026", result.Url);
    }

    [Fact]
    public async Task GetAgingAsync_RetornaBucketsCorretos()
    {
        var (db, service) = Setup();
        var clienteId = CriarCliente(db).Id;
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        db.Charges.AddRange(
            // Atual (vence hoje)
            new Charge { CompanyId = _empresaId, CustomerId = clienteId,
                Reference = "R1", Amount = 100m, DueDate = hoje,
                Status = CobrancaStatus.Pendente },
            // 1–30 dias
            new Charge { CompanyId = _empresaId, CustomerId = clienteId,
                Reference = "R2", Amount = 200m, DueDate = hoje.AddDays(-15),
                Status = CobrancaStatus.Pendente },
            // 31–60 dias
            new Charge { CompanyId = _empresaId, CustomerId = clienteId,
                Reference = "R3", Amount = 300m, DueDate = hoje.AddDays(-45),
                Status = CobrancaStatus.Pendente },
            // Paga — não deve aparecer
            new Charge { CompanyId = _empresaId, CustomerId = clienteId,
                Reference = "R4", Amount = 400m, DueDate = hoje.AddDays(-10),
                Status = CobrancaStatus.Pago }
        );
        await db.SaveChangesAsync();

        var result = await service.GetAgingAsync(default);

        Assert.Equal(100m, result.Atual);
        Assert.Equal(200m, result.Ate30Dias);
        Assert.Equal(300m, result.De31A60Dias);
        Assert.Equal(0m, result.De61A90Dias);
        Assert.Equal(0m, result.Acima90Dias);
        Assert.Equal(600m, result.Total);
        Assert.Equal(1, result.QtdAtual);
        Assert.Equal(1, result.QtdAte30Dias);
    }
}
