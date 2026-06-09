using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoRenovarServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new ContratoService(db, tenant));
    }

    private async Task<Contrato> CriarContratoAsync(AppDbContext db, ContratoStatus status, DateOnly? dataFim = null)
    {
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Ana", Whatsapp = "11999990000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Numero = 1,
            Titulo = "Serviço Mensal",
            Objeto = "Prestação",
            TipoCobranca = TipoCobranca.Recorrente,
            Valor = 500m,
            DataInicio = new DateOnly(2026, 1, 1),
            DataFim = dataFim,
            Periodicidade = Periodicidade.Mensal,
            DiaVencimento = 10,
            Status = status,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();
        return contrato;
    }

    [Fact]
    public async Task RenovarAsync_CriaNovoContratoRascunho()
    {
        var (db, svc) = Setup();
        var dataFim = new DateOnly(2026, 12, 31);
        var original = await CriarContratoAsync(db, ContratoStatus.Ativo, dataFim);

        var novo = await svc.RenovarAsync(original.Id, default);

        Assert.NotEqual(original.Id, novo.Id);
        Assert.Equal("Rascunho", novo.Status);
        Assert.Equal(new DateOnly(2027, 1, 1), novo.DataInicio);
        Assert.Null(novo.DataFim);
        Assert.Equal(original.Titulo, novo.Titulo);
        Assert.Equal(original.Valor, novo.Valor);
    }

    [Fact]
    public async Task RenovarAsync_LancaExcecao_SeNaoAtivo()
    {
        var (db, svc) = Setup();
        var contrato = await CriarContratoAsync(db, ContratoStatus.Encerrado, new DateOnly(2026, 12, 31));

        await Assert.ThrowsAsync<GestorAI.API.Shared.Exceptions.AppException>(
            () => svc.RenovarAsync(contrato.Id, default));
    }

    [Fact]
    public async Task ListVencendoAsync_RetornaContratosNoPrazo()
    {
        var (db, svc) = Setup();
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        await CriarContratoAsync(db, ContratoStatus.Ativo, hoje.AddDays(15));  // dentro do prazo
        await CriarContratoAsync(db, ContratoStatus.Ativo, hoje.AddDays(60));  // fora do prazo
        await CriarContratoAsync(db, ContratoStatus.Ativo, null);              // sem DataFim

        var vencendo = await svc.ListVencendoAsync(30, default);

        Assert.Single(vencendo);
        Assert.Equal(hoje.AddDays(15), vencendo[0].DataFim);
    }
}
