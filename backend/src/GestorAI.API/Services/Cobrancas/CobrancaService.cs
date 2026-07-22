using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Cobrancas;

public class CobrancaService(AppDbContext db, TenantContext tenantContext, AsaasService asaasService)
{
    public async Task<List<CobrancaListItem>> ListAsync(
        string? status, Guid? clienteId, string? mes, CancellationToken ct)
    {
        var query = db.Charges
            .Include(c => c.Customer)
            .Include(c => c.Contract)
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(c => c.CustomerId == clienteId.Value);

        if (mes != null && DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var mesDate))
        {
            var fimMes = mesDate.AddMonths(1).AddDays(-1);
            query = query.Where(c => c.DueDate >= mesDate && c.DueDate <= fimMes);
        }

        var list = await query.OrderBy(c => c.DueDate).ToListAsync(ct);

        if (status != null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            list = status switch
            {
                "Vencido" => list.Where(c => c.Status == CobrancaStatus.Pendente && c.DueDate < hoje).ToList(),
                _ when Enum.TryParse<CobrancaStatus>(status, out var s) => list.Where(c => c.Status == s).ToList(),
                _ => list
            };
        }

        return list.Select(ToListItem).ToList();
    }

    public async Task<CobrancaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Charges
            .Include(c => c.Customer)
            .Include(c => c.Contract)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);
        return ToResponse(c);
    }

    public async Task<CobrancaResponse> CreateAsync(CreateCobrancaRequest req, CancellationToken ct)
    {
        _ = await db.Customers.FirstOrDefaultAsync(c => c.Id == req.CustomerId, ct)
            ?? throw new AppException("Customer não encontrado.", 404);

        var cobranca = new Charge
        {
            CompanyId = tenantContext.CompanyId,
            CustomerId = req.CustomerId,
            Reference = req.Reference,
            Amount = req.Amount,
            DueDate = req.DueDate,
            Notes = req.Notes,
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync(ct);
        return await GetAsync(cobranca.Id, ct);
    }

    public async Task<CobrancaResponse> PagarAsync(Guid id, PagarCobrancaRequest req, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != CobrancaStatus.Pendente)
            throw new AppException("Apenas cobranças pendentes podem ser pagas.", 400);
        if (!Enum.TryParse<FormaPagamento>(req.PaymentMethod, out var forma))
            throw new AppException($"FormaPagamento inválida: {req.PaymentMethod}.", 400);

        c.Status = CobrancaStatus.Pago;
        c.PaymentDate = req.PaymentDate;
        c.PaymentMethod = forma;

        CriarLancamentoReceita(c, c.PaymentDate);

        await db.SaveChangesAsync(ct);

        if (c.ContractId.HasValue)
            await VerificarEncerramentoContratoAsync(c.ContractId.Value, ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == CobrancaStatus.Pago)
            throw new AppException("Cobranças pagas não podem ser excluídas.", 400);
        db.Charges.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CobrancaResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == CobrancaStatus.Pago)
            throw new AppException("Cobranças pagas não podem ser canceladas.", 400);
        c.Status = CobrancaStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<WhatsappUrlResponse> GetWhatsappUrlAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Charges
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

        var fone = new string(c.Customer!.WhatsApp.Where(char.IsDigit).ToArray());
        var msg = $"Olá {c.Customer.Name}, segue cobrança referente a {c.Reference}: " +
                  $"R$ {c.Amount:N2} com vencimento em {c.DueDate:dd/MM/yyyy}. " +
                  "Em caso de dúvidas, entre em contato.";
        var encodedMsg = Uri.EscapeDataString(msg).Replace("%20", "+");
        var url = $"https://wa.me/55{fone}?text={encodedMsg}";
        return new WhatsappUrlResponse(url);
    }

    public async Task<CobrancaResumo> GetResumoAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var aReceber = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente && c.DueDate >= hoje)
            .SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        var vencido = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente && c.DueDate < hoje)
            .SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        var recebido = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pago && c.PaymentDate >= inicioMes)
            .SumAsync(c => (decimal?)c.Amount, ct) ?? 0m;

        return new CobrancaResumo(aReceber, vencido, recebido);
    }

    public async Task<AgingResponse> GetAgingAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var pendentes = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);

        decimal atual = 0, ate30 = 0, de31a60 = 0, de61a90 = 0, acima90 = 0;
        int qAtual = 0, qAte30 = 0, qDe31a60 = 0, qDe61a90 = 0, qAcima90 = 0;

        foreach (var c in pendentes)
        {
            var diasAtraso = (hoje.ToDateTime(TimeOnly.MinValue) - c.DueDate.ToDateTime(TimeOnly.MinValue)).Days;
            if (diasAtraso <= 0)       { atual   += c.Amount; qAtual++;    }
            else if (diasAtraso <= 30) { ate30   += c.Amount; qAte30++;    }
            else if (diasAtraso <= 60) { de31a60 += c.Amount; qDe31a60++;  }
            else if (diasAtraso <= 90) { de61a90 += c.Amount; qDe61a90++;  }
            else                       { acima90 += c.Amount; qAcima90++;  }
        }

        return new AgingResponse(
            atual, ate30, de31a60, de61a90, acima90,
            atual + ate30 + de31a60 + de61a90 + acima90,
            qAtual, qAte30, qDe31a60, qDe61a90, qAcima90);
    }

    public async Task<CobrancaAsaasResponse> EnviarAsaasAsync(
        Guid id, EnviarAsaasRequest req, CancellationToken ct)
    {
        var cobranca = await db.Charges
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

        if (cobranca.Status != CobrancaStatus.Pendente)
            throw new AppException("Apenas cobranças pendentes podem ser enviadas ao Asaas.", 400);

        var config = await db.CompanySettings
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configuração da empresa não encontrada.", 404);

        if (string.IsNullOrWhiteSpace(config.AsaasApiKey))
            throw new AppException("Chave de API do Asaas não configurada. Acesse Configurações > Integrações.", 400);

        var customerId = await asaasService.GetOrCreateCustomerAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            cobranca.Customer!.Name,
            null, ct);

        var result = await asaasService.CreatePaymentAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            customerId, cobranca.Amount,
            cobranca.DueDate, cobranca.Reference,
            req.BillingType, ct);

        cobranca.AsaasId = result.Id;
        cobranca.AsaasPaymentLink = result.InvoiceUrl;
        cobranca.AsaasPixQrCode = result.PixQrCode?.Payload;
        cobranca.AsaasBoletoUrl = result.BankSlipUrl;
        await db.SaveChangesAsync(ct);

        return new CobrancaAsaasResponse(
            result.Id,
            result.InvoiceUrl,
            result.PixQrCode?.Payload,
            result.BankSlipUrl);
    }

    public async Task ConfirmarPagamentoAsaasAsync(string asaasId, string? billingType, CancellationToken ct)
    {
        var cobranca = await db.Charges
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.AsaasId == asaasId, ct);

        if (cobranca is null || cobranca.Status != CobrancaStatus.Pendente)
            return;

        cobranca.Status = CobrancaStatus.Pago;
        cobranca.PaymentDate = DateTime.UtcNow;
        cobranca.PaymentMethod = billingType switch
        {
            "PIX" => FormaPagamento.Pix,
            "CREDIT_CARD" => FormaPagamento.Cartao,
            _ => FormaPagamento.Outro,
        };

        CriarLancamentoReceita(cobranca, cobranca.PaymentDate);

        await db.SaveChangesAsync(ct);

        if (cobranca.ContractId.HasValue)
            await VerificarEncerramentoContratoAsync(cobranca.ContractId.Value, ct);
    }

    private async Task VerificarEncerramentoContratoAsync(Guid contratoId, CancellationToken ct)
    {
        var contrato = await db.Contracts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == contratoId, ct);

        if (contrato is null
            || contrato.Status != ContratoStatus.Ativo
            || contrato.ChargeType != TipoCobranca.ParceladoPrazoFixo)
            return;

        var existeAlguma = await db.Charges
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ContractId == contratoId && c.Status != CobrancaStatus.Cancelado, ct);

        if (!existeAlguma) return;

        var todasPagas = await db.Charges
            .IgnoreQueryFilters()
            .Where(c => c.ContractId == contratoId && c.Status != CobrancaStatus.Cancelado)
            .AllAsync(c => c.Status == CobrancaStatus.Pago, ct);

        if (todasPagas)
        {
            contrato.Status = ContratoStatus.Encerrado;
            await db.SaveChangesAsync(ct);
        }
    }

    private void CriarLancamentoReceita(Charge c, DateTime? dataPagamento)
    {
        db.Transactions.Add(new Transaction
        {
            CompanyId = c.CompanyId,
            Type = TipoLancamento.Receita,
            Description = c.Reference,
            Amount = c.Amount,
            DueDate = c.DueDate.ToDateTime(TimeOnly.MinValue),
            PaymentDate = dataPagamento,
            Status = StatusLancamento.Pago,
            Category = "Cobrança",
        });
    }

    private async Task<Charge> FindAsync(Guid id, CancellationToken ct) =>
        await db.Charges.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

    private static CobrancaListItem ToListItem(Charge c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DueDate < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaListItem(
            c.Id, c.Customer?.Name ?? "", c.ContractId,
            c.Contract?.Title, c.Reference, c.Amount,
            c.DueDate, statusDisplay);
    }

    private static CobrancaResponse ToResponse(Charge c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DueDate < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaResponse(
            c.Id, c.Customer?.Name ?? "", c.Customer?.WhatsApp ?? "",
            c.ContractId, c.Contract?.Title,
            c.Reference, c.Amount, c.DueDate,
            c.PaymentDate, statusDisplay,
            c.PaymentMethod?.ToString(), c.Notes, c.CreatedAt);
    }
}
