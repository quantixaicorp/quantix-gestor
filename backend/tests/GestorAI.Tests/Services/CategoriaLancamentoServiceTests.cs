using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CategoriaLancamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CategoriaLancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new CategoriaLancamentoService(db, tc));
    }

    [Fact]
    public async Task CreateAsync_CriaCategoria_ComNomeUnico()
    {
        var (_, svc) = Setup();
        var result = await svc.CreateAsync(
            new CreateCategoriaLancamentoRequest("Honorários", "Receita"), default);
        Assert.Equal("Honorários", result.Nome);
        Assert.Equal("Receita", result.Tipo);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_LancaExcecao_QuandoNomeDuplicadoNoMesmoTipo()
    {
        var (db, svc) = Setup();
        db.CategoriasLancamento.Add(new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Honorários",
            Tipo = TipoLancamento.Receita
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.CreateAsync(new CreateCategoriaLancamentoRequest("Honorários", "Receita"), default));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_RenomeiaCom_NomeValido_EAtualizaLancamentosExistentes()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Aluguel",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Aluguel março",
            Valor = 1500m,
            DataVencimento = DateTime.Today,
            Categoria = "Aluguel",
            Status = StatusLancamento.Pendente
        });
        await db.SaveChangesAsync();

        var result = await svc.UpdateAsync(cat.Id, new UpdateCategoriaLancamentoRequest("Aluguel Comercial"), default);

        Assert.Equal("Aluguel Comercial", result.Nome);
        var lancamento = await db.Lancamentos.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("Aluguel Comercial", lancamento.Categoria);
    }

    [Fact]
    public async Task DeleteAsync_RemoveCategoria_QuandoSemLancamentosVinculados()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Marketing",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(cat.Id, default);

        Assert.Empty(db.CategoriasLancamento.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoLancamentoUsaCategoria()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Marketing",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Anúncio Google",
            Valor = 300m,
            DataVencimento = DateTime.Today,
            Categoria = "Marketing",
            Status = StatusLancamento.Pendente
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.DeleteAsync(cat.Id, default));
        Assert.Equal(400, ex.StatusCode);
    }
}
