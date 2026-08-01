using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoTemplateServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoTemplateService service) Setup()
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new ContratoTemplateService(db, tc));
    }

    [Fact]
    public async Task CreateAsync_PersisteCampos()
    {
        var (db, svc) = Setup();
        var req = new CreateContratoTemplateRequest(
            "Mensalidade Padrão", "Prestação de serviços mensais",
            "Recorrente", "Mensal", 10, 500m,
            [new ContratoTemplateItemRequest("Serviço mensal", 1, 500m)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Mensalidade Padrão", result.Name);
        Assert.Equal(500m, result.DefaultAmount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ListAsync_RetornaApenasDoTenant()
    {
        var (db, svc) = Setup();
        var outroTenant = Guid.NewGuid();
        db.ContractTemplates.Add(new ContractTemplate
        {
            CompanyId = outroTenant, Name = "Outro", Subject = "X",
        });
        db.ContractTemplates.Add(new ContractTemplate
        {
            CompanyId = _empresaId, Name = "Meu", Subject = "Y",
        });
        await db.SaveChangesAsync();

        var result = await svc.ListAsync(default);

        Assert.Single(result);
        Assert.Equal("Meu", result[0].Name);
    }

    [Fact]
    public async Task DeleteAsync_Remove()
    {
        var (db, svc) = Setup();
        db.ContractTemplates.Add(new ContractTemplate
        {
            CompanyId = _empresaId, Name = "T", Subject = "O",
        });
        await db.SaveChangesAsync();
        var id = db.ContractTemplates.First().Id;

        await svc.DeleteAsync(id, default);

        Assert.Empty(db.ContractTemplates.ToList());
    }

    [Fact]
    public async Task DeleteAsync_Lanca404_QuandoNaoEncontrado()
    {
        var (_, svc) = Setup();
        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(Guid.NewGuid(), default));
    }
}
