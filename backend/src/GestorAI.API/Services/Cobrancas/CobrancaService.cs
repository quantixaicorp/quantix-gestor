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
        var query = db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Contrato)
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(c => c.ClienteId == clienteId.Value);

        if (mes != null && DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var mesDate))
        {
            var fimMes = mesDate.AddMonths(1).AddDays(-1);
            query = query.Where(c => c.DataVencimento >= mesDate && c.DataVencimento <= fimMes);
        }

        var list = await query.OrderBy(c => c.DataVencimento).ToListAsync(ct);

        if (status != null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            list = status switch
            {
                "Vencido" => list.Where(c => c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje).ToList(),
                _ when Enum.TryParse<CobrancaStatus>(status, out var s) => list.Where(c => c.Status == s).ToList(),
                _ => list
            };
        }

        return list.Select(ToListItem).ToList();
    }

    public async Task<CobrancaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Contrato)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);
        return ToResponse(c);
    }

    public async Task<CobrancaResponse> CreateAsync(CreateCobrancaRequest req, CancellationToken ct)
    {
        _ = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId, ct)
            ?? throw new AppException("Cliente não encontrado.", 404);

        var cobranca = new Cobranca
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = req.ClienteId,
            Referencia = req.Referencia,
            Valor = req.Valor,
            DataVencimento = req.DataVencimento,
            Observacao = req.Observacao,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);
        return await GetAsync(cobranca.Id, ct);
    }

    public async Task<CobrancaResponse> PagarAsync(Guid id, PagarCobrancaRequest req, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != CobrancaStatus.Pendente)
            throw new AppException("Apenas cobranças pendentes podem ser pagas.", 400);
        if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var forma))
            throw new AppException($"FormaPagamento inválida: {req.FormaPagamento}.", 400);

        c.Status = CobrancaStatus.Pago;
        c.DataPagamento = req.DataPagamento;
        c.FormaPagamento = forma;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
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
        var c = await db.Cobrancas
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

        var fone = new string(c.Cliente!.Whatsapp.Where(char.IsDigit).ToArray());
        var msg = $"Olá {c.Cliente.Nome}, segue cobrança referente a {c.Referencia}: " +
                  $"R$ {c.Valor:N2} com vencimento em {c.DataVencimento:dd/MM/yyyy}. " +
                  "Em caso de dúvidas, entre em contato.";
        var encodedMsg = Uri.EscapeDataString(msg).Replace("%20", "+");
        var url = $"https://wa.me/55{fone}?text={encodedMsg}";
        return new WhatsappUrlResponse(url);
    }

    public async Task<AgingResponse> GetAgingAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var pendentes = await db.Cobrancas
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);

        decimal atual = 0, ate30 = 0, de31a60 = 0, de61a90 = 0, acima90 = 0;
        int qAtual = 0, qAte30 = 0, qDe31a60 = 0, qDe61a90 = 0, qAcima90 = 0;

        foreach (var c in pendentes)
        {
            var diasAtraso = (hoje.ToDateTime(TimeOnly.MinValue) - c.DataVencimento.ToDateTime(TimeOnly.MinValue)).Days;
            if (diasAtraso <= 0)       { atual   += c.Valor; qAtual++;    }
            else if (diasAtraso <= 30) { ate30   += c.Valor; qAte30++;    }
            else if (diasAtraso <= 60) { de31a60 += c.Valor; qDe31a60++;  }
            else if (diasAtraso <= 90) { de61a90 += c.Valor; qDe61a90++;  }
            else                       { acima90 += c.Valor; qAcima90++;  }
        }

        return new AgingResponse(
            atual, ate30, de31a60, de61a90, acima90,
            atual + ate30 + de31a60 + de61a90 + acima90,
            qAtual, qAte30, qDe31a60, qDe61a90, qAcima90);
    }

    public async Task<CobrancaAsaasResponse> EnviarAsaasAsync(
        Guid id, EnviarAsaasRequest req, CancellationToken ct)
    {
        var cobranca = await db.Cobrancas
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

        if (cobranca.Status != CobrancaStatus.Pendente)
            throw new AppException("Apenas cobranças pendentes podem ser enviadas ao Asaas.", 400);

        var config = await db.ConfiguracoesEmpresa
            .FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configuração da empresa não encontrada.", 404);

        if (string.IsNullOrWhiteSpace(config.AsaasApiKey))
            throw new AppException("Chave de API do Asaas não configurada. Acesse Configurações > Integrações.", 400);

        var customerId = await asaasService.GetOrCreateCustomerAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            cobranca.Cliente!.Nome,
            null, ct);

        var result = await asaasService.CreatePaymentAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            customerId, cobranca.Valor,
            cobranca.DataVencimento, cobranca.Referencia,
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

    private async Task<Cobranca> FindAsync(Guid id, CancellationToken ct) =>
        await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

    private static CobrancaListItem ToListItem(Cobranca c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaListItem(
            c.Id, c.Cliente?.Nome ?? "", c.ContratoId,
            c.Contrato?.Titulo, c.Referencia, c.Valor,
            c.DataVencimento, statusDisplay);
    }

    private static CobrancaResponse ToResponse(Cobranca c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaResponse(
            c.Id, c.Cliente?.Nome ?? "", c.Cliente?.Whatsapp ?? "",
            c.ContratoId, c.Contrato?.Titulo,
            c.Referencia, c.Valor, c.DataVencimento,
            c.DataPagamento, statusDisplay,
            c.FormaPagamento?.ToString(), c.Observacao, c.CriadoEm);
    }
}
