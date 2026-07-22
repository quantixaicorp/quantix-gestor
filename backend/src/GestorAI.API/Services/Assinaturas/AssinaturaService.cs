using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Assinaturas;

public class AssinaturaService(
    AppDbContext db,
    TenantContext tenantContext,
    CobrancaService cobrancaService)
{
    public async Task<List<AssinaturaListItem>> ListAsync(Guid? planoId, string? status, CancellationToken ct)
    {
        var query = db.CustomerSubscriptions
            .Include(a => a.Customer)
            .Include(a => a.Plan)
            .AsQueryable();

        if (planoId.HasValue)
            query = query.Where(a => a.SubscriptionPlanId == planoId.Value);

        if (status != null && Enum.TryParse<AssinaturaStatus>(status, out var s))
            query = query.Where(a => a.Status == s);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AssinaturaListItem(
                a.Id, a.Customer!.Name, a.Plan!.Name,
                a.Status.ToString(), a.RenewalDate, a.CurrentCycle))
            .ToListAsync(ct);
    }

    public async Task<AssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.CustomerSubscriptions
            .Include(x => x.Customer)
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        return new AssinaturaResponse(
            a.Id, a.Customer!.Name, a.Customer.WhatsApp,
            a.SubscriptionPlanId, a.Plan!.Name, a.Plan.Price,
            a.Status.ToString(), a.StartDate, a.RenewalDate,
            a.CurrentCycle, a.ContractId);
    }

    public async Task<AssinarResponse> AssinarAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.CompanyId = empresaId;

        var (assinaturaId, contratoId, cobrancaId) = await AssinarSemAsaasAsync(empresaId, planoId, req, ct);

        var asaasResult = await cobrancaService.EnviarAsaasAsync(
            cobrancaId, new EnviarAsaasRequest("PIX"), ct);

        var cobranca = await db.Charges.FindAsync([cobrancaId], ct);
        return new AssinarResponse(
            assinaturaId, contratoId, cobrancaId,
            asaasResult.PixQrCode, asaasResult.BoletoUrl,
            cobranca!.Amount,
            cobranca.DueDate);
    }

    public async Task<(Guid AssinaturaId, Guid ContractId, Guid ChargeId)> AssinarSemAsaasAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.CompanyId = empresaId;

        var plano = await db.SubscriptionPlans.Include(p => p.Items)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == planoId && p.CompanyId == empresaId && p.IsActive, ct)
            ?? throw new AppException("Plan não encontrado.", 404);

        var cliente = await db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == empresaId && c.WhatsApp == req.WhatsApp, ct);
        if (cliente is null)
        {
            cliente = new Customer { CompanyId = empresaId, Name = req.Name, WhatsApp = req.WhatsApp, Email = req.Email };
            db.Customers.Add(cliente);
            await db.SaveChangesAsync(ct);
        }

        var numero = (await db.Contracts.IgnoreQueryFilters()
            .Where(c => c.CompanyId == empresaId)
            .MaxAsync(c => (int?)c.Number, ct) ?? 0) + 1;

        var objeto = string.Join(", ", plano.Items.Select(i =>
            i.QuantityPerCycle == 0 ? $"{i.Description} (ilimitado)" : $"{i.QuantityPerCycle}x {i.Description}"));

        var contrato = new Contract
        {
            CompanyId = empresaId,
            Number = numero,
            CustomerId = cliente.Id,
            Title = $"Assinatura — {plano.Name}",
            Subject = objeto,
            ChargeType = TipoCobranca.Recorrente,
            Amount = plano.Price,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Frequency = plano.Frequency,
            DueDay = DateTime.UtcNow.Day,
            Status = ContratoStatus.Ativo,
        };
        contrato.Items.Add(new ContractItem
        {
            Description = plano.Name,
            Quantity = 1,
            UnitPrice = plano.Price,
        });
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync(ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var assinatura = new CustomerSubscription
        {
            CompanyId = empresaId,
            CustomerId = cliente.Id,
            SubscriptionPlanId = plano.Id,
            ContractId = contrato.Id,
            StartDate = hoje,
            RenewalDate = hoje.AddMonths(1),
        };
        db.CustomerSubscriptions.Add(assinatura);

        contrato.CustomerSubscriptionId = assinatura.Id;

        var cobranca = new Charge
        {
            CompanyId = empresaId,
            CustomerId = cliente.Id,
            ContractId = contrato.Id,
            Reference = $"Assinatura {plano.Name} — {hoje:MMMM/yyyy}",
            Amount = plano.Price,
            DueDate = hoje.AddDays(3),
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync(ct);

        return (assinatura.Id, contrato.Id, cobranca.Id);
    }

    public async Task CancelarAsync(Guid id, CancellationToken ct)
    {
        var assinatura = await db.CustomerSubscriptions
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        assinatura.Status = AssinaturaStatus.Cancelada;

        var contrato = await db.Contracts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == assinatura.ContractId, ct);
        if (contrato != null && contrato.Status == ContratoStatus.Ativo)
            contrato.Status = ContratoStatus.Encerrado;

        var cobrancasPendentes = await db.Charges.IgnoreQueryFilters()
            .Where(c => c.ContractId == assinatura.ContractId && c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);
        foreach (var cob in cobrancasPendentes)
            cob.Status = CobrancaStatus.Cancelado;

        await db.SaveChangesAsync(ct);
    }
}
