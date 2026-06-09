using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CobrancaAutoEncerramentoTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CobrancaService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        var svc = new CobrancaService(db, tc, null!);
        return (db, svc);
    }

    private async Task<(Contrato contrato, Cobranca c1, Cobranca c2)> CriarSetupParceladoAsync(AppDbContext db)
    {
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Fernanda", Whatsapp = "11966660000" };
        db.Clientes.Add(cliente);

        var contrato = new Contrato
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Numero = 1,
            Titulo = "Projeto X",
            Objeto = "Desenvolvimento",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo,
            Valor = 200m,
            DataInicio = new DateOnly(2026, 1, 1),
            DataFim = new DateOnly(2026, 2, 28),
            Periodicidade = Periodicidade.Mensal,
            DiaVencimento = 1,
            Status = ContratoStatus.Ativo,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var c1 = new Cobranca
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            ContratoId = contrato.Id,
            Referencia = "Parcela 1/2",
            Valor = 100m,
            DataVencimento = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pago,
            DataPagamento = DateTime.UtcNow,
            FormaPagamento = FormaPagamento.Dinheiro,
        };
        var c2 = new Cobranca
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            ContratoId = contrato.Id,
            Referencia = "Parcela 2/2",
            Valor = 100m,
            DataVencimento = new DateOnly(2026, 2, 1),
            Status = CobrancaStatus.Pendente,
        };
        db.Cobrancas.AddRange(c1, c2);
        await db.SaveChangesAsync();

        return (contrato, c1, c2);
    }

    [Fact]
    public async Task PagarAsync_EncerraContrato_QuandoTodasParcelasPagas()
    {
        var (db, svc) = Setup();
        var (contrato, _, c2) = await CriarSetupParceladoAsync(db);

        await svc.PagarAsync(c2.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(
            DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contratos.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_NaoEncerraContrato_RecorrenteQuitado()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Lucas", Whatsapp = "11955550000" };
        db.Clientes.Add(cliente);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Numero = 2,
            Titulo = "Mensalidade",
            Objeto = "Serviços",
            TipoCobranca = TipoCobranca.Recorrente,
            Valor = 100m,
            DataInicio = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal,
            DiaVencimento = 1,
            Status = ContratoStatus.Ativo,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            ContratoId = contrato.Id,
            Referencia = "Mensalidade Jan",
            Valor = 100m,
            DataVencimento = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        await svc.PagarAsync(cobranca.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(
            DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contratos.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Ativo, contratoAtualizado.Status);
    }
}
