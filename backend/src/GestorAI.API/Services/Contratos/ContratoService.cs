using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Shared;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contratos;

public class ContratoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ContratoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var query = db.Contracts.Include(c => c.Customer).AsQueryable();
        if (status != null && Enum.TryParse<ContratoStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => ToListItem(c))
            .ToListAsync(ct);
    }

    public async Task<ContratoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contract não encontrado.", 404);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CreateAsync(CreateContratoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoCobranca>(req.ChargeType, out var tipo))
            throw new AppException($"TipoCobranca inválido: {req.ChargeType}.", 400);
        if (!Enum.TryParse<Periodicidade>(req.Frequency, out var periodicidade))
            throw new AppException($"Periodicidade inválida: {req.Frequency}.", 400);
        if (req.DueDay < 1 || req.DueDay > 28)
            throw new AppException("DueDay deve ser entre 1 e 28.", 400);

        _ = await db.Customers.FirstOrDefaultAsync(c => c.Id == req.CustomerId, ct)
            ?? throw new AppException("Customer não encontrado.", 404);

        var numero = (await db.Contracts.MaxAsync(c => (int?)c.Number, ct) ?? 0) + 1;

        var contrato = new Contract
        {
            CompanyId = tenantContext.CompanyId,
            Number = numero,
            CustomerId = req.CustomerId,
            Title = req.Title,
            Subject = req.Subject,
            ChargeType = tipo,
            Amount = req.Amount,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Frequency = periodicidade,
            DueDay = req.DueDay,
            Notes = req.Notes,
        };

        foreach (var item in req.Items)
            contrato.Items.Add(new ContractItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });

        db.Contracts.Add(contrato);
        await db.SaveChangesAsync(ct);
        return await GetAsync(contrato.Id, ct);
    }

    public async Task<ContratoResponse> AtivarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser ativados.", 400);
        if (!c.Items.Any())
            throw new AppException("Contract precisa ter pelo menos um item.", 400);
        if (c.ChargeType == TipoCobranca.ParceladoPrazoFixo && c.EndDate == null)
            throw new AppException("Contratos parcelados requerem EndDate.", 400);
        c.Status = ContratoStatus.Ativo;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> EncerrarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser encerrados.", 400);
        c.Status = ContratoStatus.Encerrado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == ContratoStatus.Encerrado || c.Status == ContratoStatus.Cancelado)
            throw new AppException("Contract já está encerrado ou cancelado.", 400);
        c.Status = ContratoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contracts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contract não encontrado.", 404);

        if (c.Status != ContratoStatus.Rascunho && c.Status != ContratoStatus.Cancelado)
            throw new AppException("Apenas contratos em rascunho ou cancelados podem ser excluídos.", 400);

        var temCobrancaPaga = await db.Charges
            .AnyAsync(cb => cb.ContractId == id && cb.Status == CobrancaStatus.Pago, ct);
        if (temCobrancaPaga)
            throw new AppException("Não é possível excluir um contrato com cobranças pagas.", 400);

        var cobrancasPendentes = await db.Charges
            .Where(cb => cb.ContractId == id)
            .ToListAsync(ct);
        db.Charges.RemoveRange(cobrancasPendentes);

        db.Contracts.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<CobrancaListItem>> GerarCobrancasAsync(
        Guid id, GerarCobrancasRequest req, CancellationToken ct)
    {
        var contrato = await db.Contracts
            .Include(c => c.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contract não encontrado.", 404);

        if (contrato.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem gerar cobranças.", 400);

        var vencimentos = CalcularVencimentos(contrato, req.De, req.Ate);
        var existentes = await db.Charges
            .Where(c => c.ContractId == id)
            .Select(c => c.DueDate)
            .ToListAsync(ct);

        var allVencimentos = CalcularVencimentos(contrato,
            contrato.StartDate,
            contrato.EndDate ?? req.Ate.AddYears(10));
        var totalParcelas = allVencimentos.Count;

        var novas = new List<Charge>();
        foreach (var venc in vencimentos.Where(v => !existentes.Contains(v)))
        {
            var parcela = allVencimentos.IndexOf(venc) + 1;
            var referencia = contrato.ChargeType == TipoCobranca.ParceladoPrazoFixo
                ? $"Parcela {parcela}/{totalParcelas} — {contrato.Title}"
                : GerarReferenciaRecorrente(contrato, venc);

            decimal valorCobranca;
            if (contrato.ChargeType == TipoCobranca.ParceladoPrazoFixo)
            {
                var eachValue = Math.Round(contrato.Amount / totalParcelas, 2);
                valorCobranca = parcela == totalParcelas
                    ? contrato.Amount - eachValue * (totalParcelas - 1)
                    : eachValue;
            }
            else
            {
                valorCobranca = contrato.Amount;
            }

            var cobranca = new Charge
            {
                CompanyId = tenantContext.CompanyId,
                CustomerId = contrato.CustomerId,
                ContractId = contrato.Id,
                Reference = referencia,
                Amount = valorCobranca,
                DueDate = venc,
            };
            novas.Add(cobranca);
            db.Charges.Add(cobranca);
        }

        await db.SaveChangesAsync(ct);

        return novas.Select(c => new CobrancaListItem(
            c.Id, contrato.Customer!.Name, c.ContractId,
            contrato.Title, c.Reference, c.Amount,
            c.DueDate, c.Status.ToString())).ToList();
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, string apiBase, CancellationToken ct)
    {
        var c = await db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contract não encontrado.", 404);

        var cfg = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.CompanyId == tenantContext.CompanyId, ct);

        var total = c.Items.Sum(i => i.Quantity * i.UnitPrice);
        var linhas = string.Join("", c.Items.Select(i =>
            $"<tr><td>{i.Description}</td><td>{i.Quantity:N2}</td>" +
            $"<td>R$ {i.UnitPrice:N2}</td><td>R$ {i.Quantity * i.UnitPrice:N2}</td></tr>"));

        var corpo = $$"""
            <h1>CONTRATO {{c.Number:D3}} — {{c.Title}}</h1>
            <div class="meta">
              Customer: {{c.Customer?.Name}}<br>
              Início: {{c.StartDate:dd/MM/yyyy}}{{(c.EndDate.HasValue ? $" | Término: {c.EndDate:dd/MM/yyyy}" : "")}}<br>
              Type: {{c.ChargeType}} | Periodicidade: {{c.Frequency}} | Amount: R$ {{c.Amount:N2}}
            </div>
            <h2 style="font-size:14px">Subject</h2>
            <div class="objeto">{{c.Subject}}</div>
            <h2 style="font-size:14px">Items</h2>
            <table>
              <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
              <tbody>{{linhas}}</tbody>
            </table>
            <div class="total">Total: R$ {{total:N2}}</div>
            <div class="assinatura">
              <div>Contratante<br>{{c.Customer?.Name}}</div>
              <div>Contratada<br>{{cfg?.NomeFantasia ?? cfg?.RazaoSocial ?? ""}}</div>
            </div>
            """;

        return HtmlDocumentoBase.WrapDocument($"CONTRATO {c.Number:D3}", corpo, cfg, apiBase);
    }

    private static List<DateOnly> CalcularVencimentos(Contract contrato, DateOnly de, DateOnly ate)
    {
        var dia = contrato.DueDay;
        var daysInStartMonth = DateTime.DaysInMonth(de.Year, de.Month);
        var primeiro = new DateOnly(de.Year, de.Month, Math.Min(dia, daysInStartMonth));
        if (primeiro < de)
            primeiro = AvançarPeriodo(primeiro, contrato.Frequency, dia);

        var vencimentos = new List<DateOnly>();
        var cursor = primeiro;
        var limite = contrato.EndDate.HasValue && contrato.EndDate.Value < ate
            ? contrato.EndDate.Value
            : ate;

        while (cursor <= limite)
        {
            vencimentos.Add(cursor);
            cursor = AvançarPeriodo(cursor, contrato.Frequency, dia);
        }

        return vencimentos;
    }

    private static DateOnly AvançarPeriodo(DateOnly data, Periodicidade periodicidade, int dia)
    {
        var meses = periodicidade switch
        {
            Periodicidade.Mensal => 1,
            Periodicidade.Trimestral => 3,
            Periodicidade.Semestral => 6,
            Periodicidade.Anual => 12,
            _ => 1
        };
        var next = data.AddMonths(meses);
        return new DateOnly(next.Year, next.Month, Math.Min(dia, DateTime.DaysInMonth(next.Year, next.Month)));
    }

    private static string GerarReferenciaRecorrente(Contract contrato, DateOnly vencimento)
    {
        var cultura = new System.Globalization.CultureInfo("pt-BR");
        var periodo = contrato.Frequency switch
        {
            Periodicidade.Mensal => $"Mensalidade {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Trimestral => $"Trimestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Semestral => $"Semestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Anual => $"Anuidade {vencimento.Year}",
            _ => vencimento.ToString("MMM/yyyy", cultura)
        };
        return $"{periodo} — {contrato.Title}";
    }

    public async Task<ContratoResponse> RenovarAsync(Guid id, CancellationToken ct)
    {
        var original = await FindAsync(id, ct);
        if (original.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser renovados.", 400);

        var novaDataInicio = original.EndDate.HasValue
            ? original.EndDate.Value.AddDays(1)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var numero = (await db.Contracts.MaxAsync(c => (int?)c.Number, ct) ?? 0) + 1;

        var novo = new Contract
        {
            CompanyId = tenantContext.CompanyId,
            Number = numero,
            CustomerId = original.CustomerId,
            Title = original.Title,
            Subject = original.Subject,
            ChargeType = original.ChargeType,
            Amount = original.Amount,
            StartDate = novaDataInicio,
            EndDate = null,
            Frequency = original.Frequency,
            DueDay = original.DueDay,
            Notes = original.Notes,
            Status = ContratoStatus.Rascunho,
        };

        foreach (var item in original.Items)
            novo.Items.Add(new ContractItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });

        db.Contracts.Add(novo);
        await db.SaveChangesAsync(ct);
        return await GetAsync(novo.Id, ct);
    }

    public async Task<List<ContratoVencendoItem>> ListVencendoAsync(int dias, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var limite = hoje.AddDays(dias);

        return await db.Contracts
            .Include(c => c.Customer)
            .Where(c => c.Status == ContratoStatus.Ativo
                     && c.EndDate.HasValue
                     && c.EndDate.Value >= hoje
                     && c.EndDate.Value <= limite)
            .Select(c => new ContratoVencendoItem(
                c.Id, c.Number, c.Customer!.Name, c.Title, c.EndDate!.Value, c.Amount))
            .ToListAsync(ct);
    }

    private async Task<Contract> FindAsync(Guid id, CancellationToken ct)
    {
        return await db.Contracts
            .Include(c => c.Customer)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contract não encontrado.", 404);
    }

    private static ContratoListItem ToListItem(Contract c) => new(
        c.Id, c.Number, c.Customer?.Name ?? "", c.Title,
        c.ChargeType.ToString(), c.Amount, c.Status.ToString(),
        c.StartDate, c.EndDate);

    private static ContratoResponse ToResponse(Contract c) => new(
        c.Id, c.Number, c.Customer?.Name ?? "", c.Customer?.WhatsApp ?? "",
        c.Title, c.Subject, c.ChargeType.ToString(), c.Amount,
        c.StartDate, c.EndDate, c.Frequency.ToString(),
        c.DueDay, c.Status.ToString(), c.Notes, c.CreatedAt,
        c.Items.Select(i => new ContratoItemResponse(i.Id, i.Description, i.Quantity, i.UnitPrice)).ToList(),
        c.Items.Sum(i => i.Quantity * i.UnitPrice),
        c.ClickSignStatus,
        c.ClickSignViewerUrl);

    public async Task<EnviarAssinaturaResponse> EnviarAssinaturaAsync(
        Guid id, EnviarAssinaturaRequest req, ClickSignService clickSignService, CancellationToken ct)
    {
        var contrato = await FindAsync(id, ct);
        if (contrato.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser enviados para assinatura.", 400);

        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configuração da empresa não encontrada.", 404);

        if (string.IsNullOrWhiteSpace(config.ClickSignApiKey))
            throw new AppException("API Key do ClickSign não configurada. Acesse Configurações → Integrações.", 400);

        var html = await GetPdfHtmlAsync(id, "", ct);
        var pdfBytes = await GerarPdfAsync(html);

        var nomeArquivo = $"contrato-{contrato.Number:D3}.pdf";
        var docResult = await clickSignService.CriarDocumentoAsync(
            config.ClickSignApiKey, config.ClickSignSandbox, nomeArquivo, pdfBytes, ct);

        await clickSignService.AdicionarSignatarioAsync(
            config.ClickSignApiKey, config.ClickSignSandbox,
            docResult.DocKey, contrato.Customer!.Name, req.EmailSignatario, ct);

        contrato.ClickSignDocKey = docResult.DocKey;
        contrato.ClickSignStatus = "Pendente";
        contrato.ClickSignViewerUrl = docResult.ViewerUrl;
        await db.SaveChangesAsync(ct);

        return new EnviarAssinaturaResponse(docResult.DocKey, docResult.ViewerUrl, "Pendente");
    }

    private static async Task<byte[]> GerarPdfAsync(string html)
    {
        using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new PuppeteerSharp.LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"],
        });
        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        return await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
        {
            Format = PuppeteerSharp.Media.PaperFormat.A4,
            PrintBackground = true,
        });
    }
}
