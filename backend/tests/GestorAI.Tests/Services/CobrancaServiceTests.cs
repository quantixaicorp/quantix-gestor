using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
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
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new CobrancaService(db, tenant));
    }

    private Cliente CriarCliente(AppDbContext db)
    {
        var c = new Cliente { EmpresaId = _empresaId, Nome = "Ana", Whatsapp = "11988880000" };
        db.Clientes.Add(c);
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
        Assert.Equal(300m, result.Valor);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPago()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Test", Valor = 100m,
            DataVencimento = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Pendente
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        var req = new PagarCobrancaRequest(DateTime.UtcNow, "Pix");
        var result = await svc.PagarAsync(cobranca.Id, req, default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal("Pix", result.FormaPagamento);
        Assert.NotNull(result.DataPagamento);
    }

    [Fact]
    public async Task PagarAsync_QuandoCancelado_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Test", Valor = 100m,
            DataVencimento = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Cancelado
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.PagarAsync(cobranca.Id, new PagarCobrancaRequest(DateTime.UtcNow, "Pix"), default));
    }

    [Fact]
    public async Task GetWhatsappUrlAsync_RetornaUrlCorreta()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db); // Whatsapp = "11988880000"
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Mensalidade Jun/2026", Valor = 300m,
            DataVencimento = new DateOnly(2026, 6, 10),
        };
        db.Cobrancas.Add(cobranca);
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

        db.Cobrancas.AddRange(
            // Atual (vence hoje)
            new Cobranca { EmpresaId = _empresaId, ClienteId = clienteId,
                Referencia = "R1", Valor = 100m, DataVencimento = hoje,
                Status = CobrancaStatus.Pendente },
            // 1–30 dias
            new Cobranca { EmpresaId = _empresaId, ClienteId = clienteId,
                Referencia = "R2", Valor = 200m, DataVencimento = hoje.AddDays(-15),
                Status = CobrancaStatus.Pendente },
            // 31–60 dias
            new Cobranca { EmpresaId = _empresaId, ClienteId = clienteId,
                Referencia = "R3", Valor = 300m, DataVencimento = hoje.AddDays(-45),
                Status = CobrancaStatus.Pendente },
            // Paga — não deve aparecer
            new Cobranca { EmpresaId = _empresaId, ClienteId = clienteId,
                Referencia = "R4", Valor = 400m, DataVencimento = hoje.AddDays(-10),
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
