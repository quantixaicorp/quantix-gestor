using System.Globalization;
using System.Text;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class AccountingExportService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<(byte[] Content, string FileName, string ContentType)> ExportAsync(
        AccountingExportRequest req, CancellationToken ct)
    {
        // Validate cash account configured
        var settings = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configurações da empresa não encontradas.", 404);

        if (settings.DefaultCashAccountId == null)
            throw new AppException("Conta padrão de caixa não configurada.", 400);

        var cashAccount = await db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == settings.DefaultCashAccountId.Value, ct)
            ?? throw new AppException("Conta padrão de caixa não encontrada no Plano de Contas.", 404);

        // Parse months into date ranges
        var periods = req.Months
            .Select(m => DateOnly.ParseExact(m, "yyyy-MM", null))
            .Select(d => (From: d, To: d.AddMonths(1).AddDays(-1)))
            .ToList();

        var fromDate = periods.Min(p => p.From).ToDateTime(TimeOnly.MinValue);
        var toDate = periods.Max(p => p.To).ToDateTime(TimeOnly.MaxValue);

        // Fetch transactions
        var statusFilter = req.IncludePending
            ? new[] { StatusLancamento.Pago, StatusLancamento.Pendente }
            : new[] { StatusLancamento.Pago };

        var transactions = await db.Transactions
            .Where(t => statusFilter.Contains(t.Status)
                     && t.PaymentDate >= fromDate
                     && t.PaymentDate <= toDate)
            .OrderBy(t => t.PaymentDate)
            .ToListAsync(ct);

        // Validate all categories are mapped
        var mappings = await db.AccountMappings
            .Include(m => m.Account)
            .ToListAsync(ct);
        var mappingDict = mappings.ToDictionary(m => m.CategoryName);

        var unmapped = transactions
            .Select(t => t.Category)
            .Distinct()
            .Where(c => !string.IsNullOrEmpty(c) && !mappingDict.ContainsKey(c))
            .ToList();

        if (unmapped.Count > 0)
            throw new AppException(
                $"As seguintes categorias não têm mapeamento contábil: {string.Join(", ", unmapped)}", 400);

        // Build accounting entries
        var entries = transactions.Select(t =>
        {
            var account = mappingDict[t.Category].Account!;
            var date = DateOnly.FromDateTime(t.PaymentDate!.Value);
            var rawDesc = !string.IsNullOrEmpty(t.Description) ? t.Description : t.Category ?? string.Empty;
            var desc = rawDesc.Length > 40 ? rawDesc[..40] : rawDesc;

            return t.Type == TipoLancamento.Receita
                ? new AccountingEntry(date, desc, cashAccount.Code, account.Code, t.Amount)
                : new AccountingEntry(date, desc, account.Code, cashAccount.Code, t.Amount);
        }).ToList();

        var exporter = req.System == AccountingSystem.Dominio
            ? (IAccountingExporter)new DominioExporter()
            : new FortesExporter();

        var from = periods.Min(p => p.From);
        var to = periods.Max(p => p.To);
        var content = exporter.Export(entries);
        return (content, exporter.FileName(from, to), exporter.ContentType);
    }
}

public interface IAccountingExporter
{
    string ContentType { get; }
    string FileName(DateOnly from, DateOnly to);
    byte[] Export(IEnumerable<AccountingEntry> entries);
}

public class DominioExporter : IAccountingExporter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public string ContentType => "text/plain";

    public string FileName(DateOnly from, DateOnly to) =>
        $"dominio_{from:yyyyMM}_{to:yyyyMM}.txt";

    public byte[] Export(IEnumerable<AccountingEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            var valor = e.Amount.ToString("N2", PtBr).Replace(".", string.Empty);
            sb.AppendLine($"I|{e.Date:dd/MM/yyyy}|{e.Description}|{e.DebitCode}|{e.CreditCode}|{valor}|");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

public class FortesExporter : IAccountingExporter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public string ContentType => "text/csv";

    public string FileName(DateOnly from, DateOnly to) =>
        $"fortes_{from:yyyyMM}_{to:yyyyMM}.csv";

    public byte[] Export(IEnumerable<AccountingEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Data;Historico;Debito;Credito;Valor");
        foreach (var e in entries)
        {
            var valor = e.Amount.ToString("N2", PtBr).Replace(".", string.Empty);
            sb.AppendLine($"{e.Date:dd/MM/yyyy};{e.Description};{e.DebitCode};{e.CreditCode};{valor}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
