using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class ChartOfAccountService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ChartOfAccountResponse>> ListAsync(CancellationToken ct)
    {
        var all = await db.ChartOfAccounts
            .OrderBy(a => a.Code)
            .ToListAsync(ct);
        return BuildTree(all, null);
    }

    public async Task<ChartOfAccountResponse> CreateAsync(CreateChartOfAccountRequest req, CancellationToken ct)
    {
        var account = new ChartOfAccount
        {
            CompanyId = tenantContext.CompanyId,
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            ParentId = req.ParentId,
        };
        db.ChartOfAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return ToResponse(account, []);
    }

    public async Task<ChartOfAccountResponse> UpdateAsync(Guid id, UpdateChartOfAccountRequest req, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Conta não encontrada.", 404);
        account.Code = req.Code;
        account.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return ToResponse(account, []);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Conta não encontrada.", 404);
        account.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task LoadTemplateAsync(CancellationToken ct)
    {
        var hasAny = await db.ChartOfAccounts.AnyAsync(ct);
        if (hasAny) return;

        var entries = GetTemplate();
        var idMap = new Dictionary<string, Guid>();

        foreach (var (code, name, type, parentCode) in entries)
        {
            Guid? parentId = parentCode != null && idMap.TryGetValue(parentCode, out var pid) ? pid : null;
            var account = new ChartOfAccount
            {
                CompanyId = tenantContext.CompanyId,
                Code = code,
                Name = name,
                Type = type,
                ParentId = parentId,
            };
            db.ChartOfAccounts.Add(account);
            await db.SaveChangesAsync(ct);
            idMap[code] = account.Id;
        }
    }

    private static List<ChartOfAccountResponse> BuildTree(List<ChartOfAccount> all, Guid? parentId) =>
        all.Where(a => a.ParentId == parentId)
           .Select(a => ToResponse(a, BuildTree(all, a.Id)))
           .ToList();

    private static ChartOfAccountResponse ToResponse(ChartOfAccount a, List<ChartOfAccountResponse> children) =>
        new(a.Id, a.Code, a.Name, a.Type, a.ParentId, a.IsActive, children);

    private static List<(string Code, string Name, AccountType Type, string? ParentCode)> GetTemplate() =>
    [
        ("1",       "Ativo",                            AccountType.Ativo,             null),
        ("1.1",     "Ativo Circulante",                 AccountType.Ativo,             "1"),
        ("1.1.1",   "Caixa e Equivalentes de Caixa",   AccountType.Ativo,             "1.1"),
        ("1.1.1.01","Caixa",                            AccountType.Ativo,             "1.1.1"),
        ("1.1.1.02","Banco Conta Corrente",             AccountType.Ativo,             "1.1.1"),
        ("1.1.2",   "Contas a Receber",                 AccountType.Ativo,             "1.1"),
        ("1.1.2.01","Clientes",                         AccountType.Ativo,             "1.1.2"),
        ("1.1.3",   "Estoques",                         AccountType.Ativo,             "1.1"),
        ("1.2",     "Ativo Não Circulante",             AccountType.Ativo,             "1"),
        ("1.2.1",   "Imobilizado",                      AccountType.Ativo,             "1.2"),
        ("2",       "Passivo",                          AccountType.Passivo,           null),
        ("2.1",     "Passivo Circulante",               AccountType.Passivo,           "2"),
        ("2.1.1",   "Fornecedores",                     AccountType.Passivo,           "2.1"),
        ("2.1.2",   "Obrigações Fiscais",               AccountType.Passivo,           "2.1"),
        ("2.1.3",   "Obrigações Trabalhistas",          AccountType.Passivo,           "2.1"),
        ("2.2",     "Passivo Não Circulante",           AccountType.Passivo,           "2"),
        ("3",       "Patrimônio Líquido",               AccountType.PatrimonioLiquido, null),
        ("3.1",     "Capital Social",                   AccountType.PatrimonioLiquido, "3"),
        ("3.2",     "Lucros/Prejuízos Acumulados",      AccountType.PatrimonioLiquido, "3"),
        ("4",       "Receitas",                         AccountType.Receita,           null),
        ("4.1",     "Receitas Operacionais",            AccountType.Receita,           "4"),
        ("4.1.1",   "Receita de Vendas",                AccountType.Receita,           "4.1"),
        ("4.1.2",   "Receita de Serviços",              AccountType.Receita,           "4.1"),
        ("4.2",     "Outras Receitas",                  AccountType.Receita,           "4"),
        ("5",       "Despesas",                         AccountType.Despesa,           null),
        ("5.1",     "Despesas Operacionais",            AccountType.Despesa,           "5"),
        ("5.1.1",   "Despesas Administrativas",         AccountType.Despesa,           "5.1"),
        ("5.1.1.01","Aluguel",                          AccountType.Despesa,           "5.1.1"),
        ("5.1.1.02","Água e Energia Elétrica",          AccountType.Despesa,           "5.1.1"),
        ("5.1.1.03","Material de Escritório",           AccountType.Despesa,           "5.1.1"),
        ("5.1.1.04","Telefone e Internet",              AccountType.Despesa,           "5.1.1"),
        ("5.1.2",   "Despesas com Pessoal",             AccountType.Despesa,           "5.1"),
        ("5.1.2.01","Salários e Ordenados",             AccountType.Despesa,           "5.1.2"),
        ("5.1.2.02","Encargos Sociais",                 AccountType.Despesa,           "5.1.2"),
        ("5.1.2.03","Pró-Labore",                       AccountType.Despesa,           "5.1.2"),
        ("5.1.3",   "Despesas Tributárias",             AccountType.Despesa,           "5.1"),
        ("5.1.3.01","Impostos e Taxas",                 AccountType.Despesa,           "5.1.3"),
        ("5.1.3.02","Simples Nacional",                 AccountType.Despesa,           "5.1.3"),
        ("5.1.4",   "Despesas Financeiras",             AccountType.Despesa,           "5.1"),
        ("5.1.4.01","Juros e Encargos Bancários",       AccountType.Despesa,           "5.1.4"),
        ("5.1.4.02","Tarifas Bancárias",                AccountType.Despesa,           "5.1.4"),
        ("5.2",     "Custo das Mercadorias/Serviços",   AccountType.Despesa,           "5"),
        ("5.2.1",   "CMV/CSV",                          AccountType.Despesa,           "5.2"),
    ];
}
