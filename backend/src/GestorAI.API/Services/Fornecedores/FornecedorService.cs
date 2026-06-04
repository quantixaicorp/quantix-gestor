using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Fornecedores;

public class FornecedorService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<FornecedorResponse>> ListAsync(string? busca, CancellationToken ct)
    {
        var query = db.Fornecedores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(f => f.Nome.Contains(busca) ||
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)));

        return await query
            .OrderBy(f => f.Nome)
            .Select(f => ToResponse(f))
            .ToListAsync(ct);
    }

    public async Task<FornecedorResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        return ToResponse(f);
    }

    public async Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = new Fornecedor
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            CnpjCpf = req.CnpjCpf,
            Telefone = req.Telefone,
            Email = req.Email,
            Logradouro = req.Logradouro,
            Cidade = req.Cidade,
            Uf = req.Uf,
            Cep = req.Cep,
            Contato = req.Contato,
            Observacoes = req.Observacoes,
        };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task<FornecedorResponse> UpdateAsync(Guid id, UpdateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);

        fornecedor.Nome = req.Nome;
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.Telefone = req.Telefone;
        fornecedor.Email = req.Email;
        fornecedor.Logradouro = req.Logradouro;
        fornecedor.Cidade = req.Cidade;
        fornecedor.Uf = req.Uf;
        fornecedor.Cep = req.Cep;
        fornecedor.Contato = req.Contato;
        fornecedor.Observacoes = req.Observacoes;

        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync(ct);
    }

    private static FornecedorResponse ToResponse(Fornecedor f) =>
        new(f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
            f.Logradouro, f.Cidade, f.Uf, f.Cep,
            f.Contato, f.Observacoes, f.DataCadastro);
}
