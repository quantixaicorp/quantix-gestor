using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new ContratoService(db, tenant));
    }

    private Cliente CriarCliente(AppDbContext db)
    {
        var c = new Cliente { EmpresaId = _empresaId, Nome = "João", Whatsapp = "11999990000" };
        db.Clientes.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_PersistsAsRascunho()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var req = new CreateContratoRequest(
            cliente.Id, "Plano Mensal", "Serviços mensais", "Recorrente",
            500m, DateOnly.FromDateTime(DateTime.Today), null,
            "Mensal", 10, null,
            [new ContratoItemRequest("Consulta", 1, 500m)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Rascunho", result.Status);
        Assert.Equal(1, result.Numero);
        Assert.Equal(500m, result.Total);
    }

    [Fact]
    public async Task AtivarAsync_RascunhoViradoAtivo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 100m,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 5,
            Status = ContratoStatus.Rascunho
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var result = await svc.AtivarAsync(contrato.Id, default);

        Assert.Equal("Ativo", result.Status);
    }

    [Fact]
    public async Task AtivarAsync_SemItens_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 100m,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 5,
            Status = ContratoStatus.Rascunho
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.AtivarAsync(contrato.Id, default));
    }

    [Fact]
    public async Task GerarCobrancasAsync_CriaCobrancasNoPeriodo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "Mensal", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 200m,
            DataInicio = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 10,
            Status = ContratoStatus.Ativo
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        var result = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Equal(3, result.Count); // Jan, Fev, Mar
        Assert.All(result, c => Assert.Equal(200m, c.Valor));
        Assert.Equal(new DateOnly(2026, 1, 10), result[0].DataVencimento);
        Assert.Equal(new DateOnly(2026, 2, 10), result[1].DataVencimento);
        Assert.Equal(new DateOnly(2026, 3, 10), result[2].DataVencimento);
    }

    [Fact]
    public async Task GerarCobrancasAsync_NaoDuplica()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "Mensal", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 200m,
            DataInicio = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 10,
            Status = ContratoStatus.Ativo
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28));
        await svc.GerarCobrancasAsync(contrato.Id, req, default);
        var result2 = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Empty(result2); // already exist, no duplicates
        Assert.Equal(2, db.Cobrancas.Count());
    }

    [Fact]
    public async Task GerarCobrancasAsync_ParceladoPrazoFixo_ValoresSomamTotal()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        // 3 parcelas de R$ 100,00 — cada uma R$ 33,33 exceto a última (R$ 33,34)
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "Parcelado", Objeto = "O",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo, Valor = 100m,
            DataInicio = new DateOnly(2026, 1, 1),
            DataFim = new DateOnly(2026, 3, 31),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 10,
            Status = ContratoStatus.Ativo
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        var result = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Equal(3, result.Count);
        Assert.Equal(100m, result.Sum(c => c.Valor)); // total must equal contract value exactly
        Assert.Contains(result, c => c.Referencia.StartsWith("Parcela 1/3"));
        Assert.Contains(result, c => c.Referencia.StartsWith("Parcela 3/3"));
    }

    [Fact]
    public async Task AtivarAsync_ParceladoSemDataFim_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo, Valor = 100m,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 5,
            Status = ContratoStatus.Rascunho
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.AtivarAsync(contrato.Id, default));
    }
}
