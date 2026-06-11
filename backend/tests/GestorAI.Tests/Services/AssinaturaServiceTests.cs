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
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
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

    private PlanoAssinatura CriarPlano(AppDbContext db)
    {
        var plano = new PlanoAssinatura
        {
            EmpresaId = _empresaId, Nome = "Premium", Preco = 129m,
            Periodicidade = Periodicidade.Mensal, Nicho = "Barbearia"
        };
        plano.Itens.Add(new PlanoAssinaturaItem { Descricao = "Corte", QuantidadePorCiclo = 4, Tipo = TipoItemPlano.Servico });
        db.PlanosAssinatura.Add(plano);
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

        Assert.Equal(_empresaId, tenant.EmpresaId);

        var clienteExiste = await db.Clientes.IgnoreQueryFilters()
            .AnyAsync(c => c.Whatsapp == "11999990000");
        Assert.True(clienteExiste);

        var contratoExiste = await db.Contratos.IgnoreQueryFilters()
            .AnyAsync(c => c.AssinaturaClienteId == assinatura.AssinaturaId);
        Assert.True(contratoExiste);

        var cobrancaExiste = await db.Cobrancas.IgnoreQueryFilters()
            .AnyAsync(c => c.ContratoId == assinatura.ContratoId);
        Assert.True(cobrancaExiste);
    }

    [Fact]
    public async Task AssinarAsync_ClienteJaExiste_ReusaCliente()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        db.Clientes.Add(new Cliente { EmpresaId = _empresaId, Nome = "João", Whatsapp = "11999990000" });
        await db.SaveChangesAsync();

        var req = new AssinarRequest("João Silva", "11999990000", null);
        await svc.AssinarSemAsaasAsync(_empresaId, plano.Id, req, default);

        var count = await db.Clientes.IgnoreQueryFilters()
            .CountAsync(c => c.Whatsapp == "11999990000");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CancelarAsync_MarcaCanceladaEEncerraContrato()
    {
        var (db, _, svc) = Setup();
        var plano = CriarPlano(db);
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Maria", Whatsapp = "11888880000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 129m, Numero = 1,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 1, Status = ContratoStatus.Ativo
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var assinatura = new AssinaturaCliente
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id, ContratoId = contrato.Id,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            DataRenovacao = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
        };
        db.AssinaturasCliente.Add(assinatura);
        await db.SaveChangesAsync();

        await svc.CancelarAsync(assinatura.Id, default);

        var a = await db.AssinaturasCliente.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == assinatura.Id);
        Assert.Equal(AssinaturaStatus.Cancelada, a!.Status);

        var c = await db.Contratos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, c!.Status);
    }
}

public class FakeHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}
