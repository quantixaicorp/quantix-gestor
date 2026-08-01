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
        db.Customers.Add(cliente);

        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Number = 1,
            Title = "Projeto X",
            Subject = "Desenvolvimento",
            ChargeType = TipoCobranca.ParceladoPrazoFixo,
            Amount = 200m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 2, 28),
            Frequency = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.Ativo,
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();

        var c1 = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContractId = contrato.Id,
            Reference = "Parcela 1/2",
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pago,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = FormaPagamento.Dinheiro,
        };
        var c2 = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContractId = contrato.Id,
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

        var contratoAtualizado = await db.Contracts.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_NaoEncerraContrato_RecorrenteQuitado()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Lucas", WhatsApp = "11955550000" };
        db.Customers.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Number = 2,
            Title = "Mensalidade",
            Subject = "Serviços",
            ChargeType = TipoCobranca.Recorrente,
            Amount = 100m,
            StartDate = new DateOnly(2026, 1, 1),
            Frequency = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.Ativo,
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();

        var cobranca = new Charge
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            ContractId = contrato.Id,
            Reference = "Mensalidade Jan",
            Amount = 100m,
            DueDate = new DateOnly(2026, 1, 1),
            Status = CobrancaStatus.Pendente,
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        await svc.PagarAsync(cobranca.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(
            DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contracts.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Ativo, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_EncerraContrato_QuandoUmaParcelaEstaoCancelada()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Beatriz", WhatsApp = "11944440000" };
        db.Customers.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Number = 3,
            Title = "Projeto Y",
            Subject = "Design",
            ChargeType = TipoCobranca.ParceladoPrazoFixo,
            Amount = 300m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 3, 31),
            Frequency = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.Ativo,
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();

        var c1 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContractId = contrato.Id, Reference = "Parcela 1/3", Amount = 100m, DueDate = new DateOnly(2026, 1, 1), Status = CobrancaStatus.Pago, PaymentDate = DateTime.UtcNow, PaymentMethod = FormaPagamento.Dinheiro };
        var c2 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContractId = contrato.Id, Reference = "Parcela 2/3", Amount = 100m, DueDate = new DateOnly(2026, 2, 1), Status = CobrancaStatus.Cancelado };
        var c3 = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContractId = contrato.Id, Reference = "Parcela 3/3", Amount = 100m, DueDate = new DateOnly(2026, 3, 1), Status = CobrancaStatus.Pendente };
        db.Charges.AddRange(c1, c2, c3);
        await db.SaveChangesAsync();

        await svc.PagarAsync(c3.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contracts.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }

    [Fact]
    public async Task PagarAsync_NaoReprocessa_ContratoJaEncerrado()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Ricardo", WhatsApp = "11933330000" };
        db.Customers.Add(cliente);
        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Number = 4,
            Title = "Projeto Encerrado",
            Subject = "Consultoria",
            ChargeType = TipoCobranca.ParceladoPrazoFixo,
            Amount = 100m,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 1, 31),
            Frequency = Periodicidade.Mensal,
            DueDay = 1,
            Status = ContratoStatus.Encerrado,
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();

        var cobranca = new Charge { CompanyId = _empresaId, CustomerId = cliente.Id, ContractId = contrato.Id, Reference = "Parcela única", Amount = 100m, DueDate = new DateOnly(2026, 1, 1), Status = CobrancaStatus.Pendente };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        await svc.PagarAsync(cobranca.Id, new GestorAI.API.DTOs.Cobrancas.PagarCobrancaRequest(DateTime.UtcNow, "Dinheiro"), default);

        var contratoAtualizado = await db.Contracts.IgnoreQueryFilters().FirstAsync(c => c.Id == contrato.Id);
        Assert.Equal(ContratoStatus.Encerrado, contratoAtualizado.Status);
    }
}
