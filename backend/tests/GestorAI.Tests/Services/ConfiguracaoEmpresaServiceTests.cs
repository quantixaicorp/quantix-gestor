using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Fiscal;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ConfiguracaoEmpresaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ConfiguracaoEmpresaService svc) Setup()
    {
        var tenant = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new ConfiguracaoEmpresaService(db, tenant, null!));
    }

    [Fact]
    public async Task AtualizarAsync_PersisteTelefoneEmail()
    {
        var (_, svc) = Setup();
        var req = new AtualizarConfiguracaoEmpresaRequest(
            null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null,
            Phone: "11999990000", Email: "contato@empresa.com");

        var result = await svc.AtualizarAsync(req, default);

        Assert.Equal("11999990000", result.Phone);
        Assert.Equal("contato@empresa.com", result.Email);
    }

    [Fact]
    public async Task SalvarAgendamentoAsync_PersisteValores()
    {
        var (_, svc) = Setup();
        var req = new SalvarAgendamentoConfigRequest(false, 50.00m, 24);

        await svc.SalvarAgendamentoAsync(req, default);
        var result = await svc.ObterAsync(default);

        Assert.False(result.AutoApprove);
        Assert.Equal(50.00m, result.DepositAmount);
        Assert.Equal(24, result.CancellationLimitHours);
    }

    [Fact]
    public async Task ObterAsync_AprovarAutomaticamenteDefaultTrue()
    {
        var (_, svc) = Setup();

        var result = await svc.ObterAsync(default);

        Assert.True(result.AutoApprove);
    }
}
