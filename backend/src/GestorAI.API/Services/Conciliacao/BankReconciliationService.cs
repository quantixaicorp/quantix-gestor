using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Conciliacao;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Conciliacao;

public class BankReconciliationService(
    AppDbContext db,
    TenantContext tenantContext,
    BankStatementParserService parser)
{
    public async Task<BankStatementListItem> ImportAsync(
        Guid bankAccountId,
        string fileName,
        BankStatementFormat format,
        List<ParsedTransaction> transactions,
        CancellationToken ct)
    {
        var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.Id == bankAccountId, ct)
            ?? throw new AppException("Conta bancária não encontrada.", 404);

        // Prevent re-import of same file for same account
        var existingStatement = await db.BankStatements
            .FirstOrDefaultAsync(s =>
                s.BankAccountId == bankAccountId &&
                s.FileName == fileName, ct);
        if (existingStatement != null)
            throw new AppException($"Um extrato com o nome '{fileName}' já foi importado para esta conta. Exclua o existente antes de reimportar.", 409);

        var statement = new BankStatement
        {
            CompanyId = tenantContext.CompanyId,
            BankAccountId = bankAccountId,
            FileName = fileName,
            Format = format,
            PeriodStart = transactions.Count > 0 ? transactions.Min(t => t.Date) : DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEnd = transactions.Count > 0 ? transactions.Max(t => t.Date) : DateOnly.FromDateTime(DateTime.UtcNow),
            ItemCount = transactions.Count,
        };
        db.BankStatements.Add(statement);
        await db.SaveChangesAsync(ct);

        // load all company transactions for matching
        var allTx = await db.Transactions
            .Where(t => t.Status != StatusLancamento.Cancelado)
            .ToListAsync(ct);

        var usedTxIds = new HashSet<Guid>();

        foreach (var parsed in transactions)
        {
            var absAmount = Math.Abs(parsed.Amount);
            var item = new BankStatementItem
            {
                BankStatementId = statement.Id,
                Date = parsed.Date,
                Amount = absAmount,
                Description = parsed.Description,
                BankTransactionId = parsed.BankTransactionId,
            };
            db.BankStatementItems.Add(item);

            var expectedType = parsed.Amount > 0 ? TipoLancamento.Receita : TipoLancamento.Despesa;
            var parsedDateTime = parsed.Date.ToDateTime(TimeOnly.MinValue);

            // try exact match first
            var exactMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                t.Amount == absAmount &&
                Math.Abs((t.DueDate.Date - parsedDateTime).TotalDays) <= 3);

            if (exactMatch != null)
            {
                usedTxIds.Add(exactMatch.Id);
                item.Status = BankStatementItemStatus.AutoConciliated;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = exactMatch.Id,
                    ConfidenceScore = 100,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // try fuzzy match (within 1% amount + 7 days)
            var fuzzyMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                Math.Abs(t.Amount - absAmount) / Math.Max(absAmount, 0.01m) <= 0.01m &&
                Math.Abs((t.DueDate.Date - parsedDateTime).TotalDays) <= 7);

            if (fuzzyMatch != null)
            {
                usedTxIds.Add(fuzzyMatch.Id);
                var daysDiff = Math.Abs((fuzzyMatch.DueDate.Date - parsedDateTime).TotalDays);
                var score = daysDiff <= 3 ? 85 : 70;
                item.Status = BankStatementItemStatus.PendingReview;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = fuzzyMatch.Id,
                    ConfidenceScore = score,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // exact amount, date > 7 days
            var farMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                t.Amount == absAmount);

            if (farMatch != null)
            {
                usedTxIds.Add(farMatch.Id);
                item.Status = BankStatementItemStatus.PendingReview;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = farMatch.Id,
                    ConfidenceScore = 50,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // no match — create transaction automatically
            item.Status = BankStatementItemStatus.Unmatched;
            var newTx = new Transaction
            {
                CompanyId = tenantContext.CompanyId,
                Type = expectedType,
                Description = parsed.Description,
                Amount = absAmount,
                DueDate = parsedDateTime,
                PaymentDate = parsedDateTime,
                Status = StatusLancamento.Pago,
                Category = "Importado",
                Notes = "Gerado automaticamente via import de extrato bancário",
                Source = TransactionSource.BankImport,
            };
            db.Transactions.Add(newTx);
            db.BankReconciliations.Add(new BankReconciliation
            {
                BankStatementItemId = item.Id,
                TransactionId = newTx.Id,
                ConfidenceScore = 0,
                MatchType = BankMatchType.Auto,
                CreatedByImport = true,
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetStatementListItemAsync(statement.Id, ct);
    }

    public async Task<List<BankStatementListItem>> ListStatementsAsync(
        Guid? bankAccountId, CancellationToken ct)
    {
        var query = db.BankStatements
            .Include(s => s.BankAccount)
            .Include(s => s.Items)
                .ThenInclude(i => i.Reconciliation)
            .AsQueryable();

        if (bankAccountId.HasValue)
            query = query.Where(s => s.BankAccountId == bankAccountId.Value);

        var statements = await query.OrderByDescending(s => s.ImportedAt).ToListAsync(ct);
        return statements.Select(ToListItem).ToList();
    }

    public async Task<List<BankStatementItemResponse>> GetItemsAsync(
        Guid statementId, CancellationToken ct)
    {
        // Verify the statement belongs to this tenant (global filter on BankStatement scopes it)
        _ = await db.BankStatements.FirstOrDefaultAsync(s => s.Id == statementId, ct)
            ?? throw new AppException("Extrato não encontrado.", 404);

        var items = await db.BankStatementItems
            .Where(i => i.BankStatementId == statementId)
            .Include(i => i.Reconciliation)
                .ThenInclude(r => r!.Transaction)
            .OrderBy(i => i.Date)
            .ToListAsync(ct);

        return items.Select(ToItemResponse).ToList();
    }

    public async Task DeleteStatementAsync(Guid statementId, CancellationToken ct)
    {
        var stmt = await db.BankStatements
            .Include(s => s.Items)
                .ThenInclude(i => i.Reconciliation)
            .FirstOrDefaultAsync(s => s.Id == statementId, ct)
            ?? throw new AppException("Extrato não encontrado.", 404);

        // remove auto-created transactions
        var autoTxIds = stmt.Items
            .Where(i => i.Reconciliation?.CreatedByImport == true)
            .Select(i => i.Reconciliation!.TransactionId)
            .ToList();

        var txToRemove = await db.Transactions
            .Where(t => autoTxIds.Contains(t.Id))
            .ToListAsync(ct);
        db.Transactions.RemoveRange(txToRemove);
        db.BankStatements.Remove(stmt);
        await db.SaveChangesAsync(ct);
    }

    public async Task ManualMatchAsync(ManualMatchRequest req, CancellationToken ct)
    {
        var item = await db.BankStatementItems
            .Include(i => i.Reconciliation)
            .Include(i => i.BankStatement)
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct)
            ?? throw new AppException("Item não encontrado.", 404);

        // Tenant check — BankStatement global filter ensures it belongs to this tenant
        var stmt = await db.BankStatements.FirstOrDefaultAsync(s => s.Id == item.BankStatementId, ct)
            ?? throw new AppException("Acesso negado.", 403);

        var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == req.TransactionId, ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (item.Reconciliation != null)
        {
            // remove previous tentative reconciliation if CreatedByImport
            if (item.Reconciliation.CreatedByImport)
            {
                var oldTx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == item.Reconciliation.TransactionId, ct);
                if (oldTx != null) db.Transactions.Remove(oldTx);
            }
            db.BankReconciliations.Remove(item.Reconciliation);
        }

        item.Status = BankStatementItemStatus.AutoConciliated;
        db.BankReconciliations.Add(new BankReconciliation
        {
            BankStatementItemId = item.Id,
            TransactionId = tx.Id,
            ConfidenceScore = 100,
            MatchType = BankMatchType.Manual,
            CreatedByImport = false,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UndoMatchAsync(Guid reconciliationId, CancellationToken ct)
    {
        var recon = await db.BankReconciliations
            .Include(r => r.BankStatementItem)
                .ThenInclude(i => i!.BankStatement)
            .FirstOrDefaultAsync(r => r.Id == reconciliationId, ct)
            ?? throw new AppException("Conciliação não encontrada.", 404);

        // Tenant check — BankStatement has global filter; null means wrong tenant
        if (recon.BankStatementItem?.BankStatement == null)
            throw new AppException("Acesso negado.", 403);

        if (recon.CreatedByImport)
        {
            // delete the auto-generated transaction and return item to Unmatched
            var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == recon.TransactionId, ct);
            if (tx != null) db.Transactions.Remove(tx);
            recon.BankStatementItem!.Status = BankStatementItemStatus.Unmatched;
        }
        else
        {
            recon.BankStatementItem!.Status = BankStatementItemStatus.PendingReview;
        }

        db.BankReconciliations.Remove(recon);
        await db.SaveChangesAsync(ct);
    }

    public async Task IgnoreAsync(Guid itemId, CancellationToken ct)
    {
        var item = await db.BankStatementItems
            .Include(i => i.Reconciliation)
            .Include(i => i.BankStatement)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new AppException("Item não encontrado.", 404);

        // Tenant check — BankStatement has global filter; null means wrong tenant
        if (item.BankStatement == null)
            throw new AppException("Acesso negado.", 403);

        if (item.Reconciliation != null)
        {
            db.BankReconciliations.Remove(item.Reconciliation);
            if (item.Reconciliation.CreatedByImport)
            {
                var tx = await db.Transactions.FirstOrDefaultAsync(t => t.Id == item.Reconciliation.TransactionId, ct);
                if (tx != null) db.Transactions.Remove(tx);
            }
        }

        item.Status = BankStatementItemStatus.ManuallyIgnored;
        await db.SaveChangesAsync(ct);
    }

    private async Task<BankStatementListItem> GetStatementListItemAsync(
        Guid statementId, CancellationToken ct)
    {
        var stmt = await db.BankStatements
            .Include(s => s.BankAccount)
            .Include(s => s.Items)
            .FirstAsync(s => s.Id == statementId, ct);
        return ToListItem(stmt);
    }

    private static BankStatementListItem ToListItem(BankStatement s) => new(
        s.Id, s.BankAccountId, s.BankAccount?.Name ?? "",
        s.FileName, s.Format.ToString(),
        s.PeriodStart, s.PeriodEnd, s.ImportedAt, s.ItemCount,
        s.Items.Count(i => i.Status == BankStatementItemStatus.AutoConciliated),
        s.Items.Count(i => i.Status == BankStatementItemStatus.PendingReview),
        s.Items.Count(i => i.Status == BankStatementItemStatus.Unmatched),
        s.Items.Count(i => i.Status == BankStatementItemStatus.ManuallyIgnored));

    private static BankStatementItemResponse ToItemResponse(BankStatementItem i)
    {
        BankReconciliationResponse? recon = null;
        if (i.Reconciliation != null)
        {
            var r = i.Reconciliation;
            var t = r.Transaction;
            recon = new BankReconciliationResponse(
                r.Id, r.TransactionId,
                t?.Description ?? "",
                t?.Amount ?? 0,
                t?.DueDate ?? DateTime.MinValue,
                r.ConfidenceScore, r.MatchType.ToString(),
                r.CreatedByImport);
        }
        return new BankStatementItemResponse(
            i.Id, i.Date, i.Amount, i.Description,
            i.BankTransactionId, i.Status.ToString(), recon);
    }
}
