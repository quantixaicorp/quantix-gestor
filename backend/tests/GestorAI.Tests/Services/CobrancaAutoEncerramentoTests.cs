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
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        var svc = new CobrancaService(db, tc, null!);
        return (db, svc);
    }

    private async Task<(Contract contrato, Charge c1, Charge c2)> CriarSetupParceladoAsync(AppDbContext db)
    {
        var cliente = new Customer { CompanyId = _empresaId, Name = "Fernanda", WhatsApp = "11966660000" };
        db.Clientes.Add(cliente);

        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 1,
            Title = "Projeto X",
            Subject = "Desenvolvimento",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo,
            Amount = 200m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 2, 28),
            Periodicidade = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.IsActive,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var c1 = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContratoId = contrato.Id,
            Reference = "Parcela 1/2",
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pago,
            PaymentDate = DateTime.UtcNow,
            FormaPagamento = FormaPagamento.Dinheiro,
        };
        var c2 = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContratoId = contrato.Id,
            Reference = "Parcela 2/2",
            Amount = 100m,
            DueDate = new DateOnly(2026, 2, 1),
            Status = CobrancaStatus.Pendente,
        };
        db.Charges.AddRange(c1, c2);
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
        var cliente = new Customer { CompanyId = _empresaId, Name = "Lucas", WhatsApp = "11955550000" };
        db.Clientes.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 2,
            Title = "Mensalidade",
            Subject = "Serviços",
            TipoCobranca = TipoCobranca.Recorrente,
            Amount = 100m,
            StartDate = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.IsActive,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var cobranca = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContratoId = contrato.Id,
            Reference = "Mensalidade Jan",
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pendente,
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        await svc.PagarAsync(cobranca.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(
            DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contratos.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.IsActive, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_EncerraContrato_QuandoUmaParcelaEstaoCancelada()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Beatriz", WhatsApp = "11944440000" };
        db.Clientes.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 3,
            Title = "Projeto Y",
            Subject = "Design",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo,
            Amount = 300m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 3, 31),
            Periodicidade = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.IsActive,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var c1 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContratoId = contrato.Id, Reference = "Parcela 1/3", Amount = 100m, DueDate = new DateOnly(2026, 1, 1), Status = CobrancaStatus.Pago, PaymentDate = DateTime.UtcNow, FormaPagamento = FormaPagamento.Dinheiro };
        var c2 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContratoId = contrato.Id, Reference = "Parcela 2/3", Amount = 100m, DueDate = new DateOnly(2026, 2, 1), Status = CobrancaStatus.Cancelado };
        var c3 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContratoId = contrato.Id, Reference = "Parcela 3/3", Amount = 100m, DueDate = new DateOnly(2026, 3, 1), Status = CobrancaStatus.Pendente };
        db.Charges.AddRange(c1, c2, c3);
        await db.SaveChangesAsync();

        await svc.PagarAsync(c3.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contratos.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_NaoReprocessa_ContratoJaEncerrado()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Ricardo", WhatsApp = "11933330000" };
        db.Clientes.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Numero = 4,
            Title = "Projeto Encerrado",
            Subject = "Consultoria",
            TipoCobranca = TipoCobranca.ParceladoPrazoFixo,
            Amount = 100m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            Periodicidade = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.Encerrado,
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var cobranca = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContratoId = contrato.Id, Reference = "Parcela única", Amount = 100m, DueDate = new DateOnly(2026, 1, 1), Status = CobrancaStatus.Pendente };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        await svc.PagarAsync(cobranca.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contratos.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }
}
