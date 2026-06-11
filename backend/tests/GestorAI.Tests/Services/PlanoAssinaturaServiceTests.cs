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
        var tenant = new TenantContext { EmpresaId = _empresaId };
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
            "Básico", "Plano simples", "Barbearia", 79m, "Mensal", false,
            [new PlanoItemRequest("Corte", null, 2, "Servico", null)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Básico", result.Nome);
        Assert.Equal(79m, result.Preco);
        Assert.Single(result.Itens);
        Assert.Equal("Corte", result.Itens[0].Descricao);
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
        var plano = new PlanoAssinatura
        {
            EmpresaId = _empresaId, Nome = "Original", Preco = 50m,
            Periodicidade = Periodicidade.Mensal
        };
        db.PlanosAssinatura.Add(plano);
        await db.SaveChangesAsync();

        var req = new UpdatePlanoAssinaturaRequest("Atualizado", null, "X", 60m, "Mensal", false, false, []);
        var result = await svc.UpdateAsync(plano.Id, req, default);

        Assert.Equal("Atualizado", result.Nome);
        Assert.False(result.Ativo);
    }

    [Fact]
    public async Task DeleteAsync_PlanoComAssinantes_LancaExcecao()
    {
        var (db, svc) = Setup();
        var plano = new PlanoAssinatura { EmpresaId = _empresaId, Nome = "P", Preco = 99m, Periodicidade = Periodicidade.Mensal };
        db.PlanosAssinatura.Add(plano);
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "C", Whatsapp = "11999990000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 99m, Numero = 1,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 1, Status = ContratoStatus.Ativo
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();
        db.AssinaturasCliente.Add(new AssinaturaCliente
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id, ContratoId = contrato.Id,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataRenovacao = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(plano.Id, default));
    }
}
