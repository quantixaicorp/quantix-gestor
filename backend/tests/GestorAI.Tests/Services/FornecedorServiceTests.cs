using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Fornecedores;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class FornecedorServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, FornecedorService svc) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new FornecedorService(db, tenantContext));
    }

    [Fact]
    public async Task ListAsync_RetornaApenasDoTenant()
    {
        var (db, svc) = Setup();
        var outroTenant = Guid.NewGuid();

        db.Fornecedores.AddRange(
            new Fornecedor { EmpresaId = _empresaId, Nome = "Fornecedor A" },
            new Fornecedor { EmpresaId = outroTenant, Nome = "Fornecedor Outro Tenant" }
        );
        await db.SaveChangesAsync();

        var result = await svc.ListAsync(null, default);

        Assert.Single(result);
        Assert.Equal("Fornecedor A", result[0].Nome);
    }

    [Fact]
    public async Task CreateAsync_SalvaComEmpresaId()
    {
        var (db, svc) = Setup();

        var req = new CreateFornecedorRequest(
            Nome: "Distribuidora XYZ",
            CnpjCpf: "12345678000195",
            Telefone: "11999990000",
            Email: "contato@xyz.com",
            Logradouro: "Rua das Flores, 100",
            Cidade: "São Paulo",
            Uf: "SP",
            Cep: "01310-100",
            Contato: "João Silva",
            Observacoes: null);

        var result = await svc.CreateAsync(req, default);

        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await db.Fornecedores.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal(_empresaId, saved.EmpresaId);
        Assert.Equal("Distribuidora XYZ", saved.Nome);
    }

    [Fact]
    public async Task UpdateAsync_Lanca404_QuandoNaoEncontrado()
    {
        var (_, svc) = Setup();

        var req = new UpdateFornecedorRequest(
            Nome: "Novo Nome",
            CnpjCpf: null, Telefone: null, Email: null,
            Logradouro: null, Cidade: null, Uf: null,
            Cep: null, Contato: null, Observacoes: null);

        await Assert.ThrowsAsync<AppException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), req, default));
    }

    [Fact]
    public async Task DeleteAsync_RemoveFornecedor()
    {
        var (db, svc) = Setup();
        var fornecedor = new Fornecedor { EmpresaId = _empresaId, Nome = "Para Deletar" };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(fornecedor.Id, default);

        var saved = await db.Fornecedores.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == fornecedor.Id);
        Assert.Null(saved);
    }

    [Fact]
    public async Task DeleteAsync_Lanca404_QuandoNaoEncontrado()
    {
        var (_, svc) = Setup();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.DeleteAsync(Guid.NewGuid(), default));
    }
}
