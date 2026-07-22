using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
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
        var query = db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(f => f.Name.Contains(busca) ||
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)));

        return await query
            .OrderBy(f => f.Name)
            .Select(f => ToResponse(f))
            .ToListAsync(ct);
    }

    public async Task<FornecedorResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Suppliers.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Supplier não encontrado", 404);
        return ToResponse(f);
    }

    public async Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = new Supplier
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            CnpjCpf = req.CnpjCpf,
            Phone = req.Phone,
            Email = req.Email,
            Logradouro = req.Logradouro,
            City = req.City,
            Uf = req.Uf,
            Cep = req.Cep,
            ContactPerson = req.ContactPerson,
            Notes = req.Notes,
            RazaoSocial = req.RazaoSocial,
            NomeFantasia = req.NomeFantasia,
            InscricaoEstadual = req.InscricaoEstadual,
            WhatsApp = req.WhatsApp,
        };
        db.Suppliers.Add(fornecedor);
        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task<FornecedorResponse> UpdateAsync(Guid id, UpdateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = await db.Suppliers.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Supplier não encontrado", 404);

        fornecedor.Name = req.Name;
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.Phone = req.Phone;
        fornecedor.Email = req.Email;
        fornecedor.Logradouro = req.Logradouro;
        fornecedor.City = req.City;
        fornecedor.Uf = req.Uf;
        fornecedor.Cep = req.Cep;
        fornecedor.ContactPerson = req.ContactPerson;
        fornecedor.Notes = req.Notes;
        fornecedor.RazaoSocial = req.RazaoSocial;
        fornecedor.NomeFantasia = req.NomeFantasia;
        fornecedor.InscricaoEstadual = req.InscricaoEstadual;
        fornecedor.WhatsApp = req.WhatsApp;
        if (Enum.TryParse<StatusFornecedor>(req.Status, out var status))
            fornecedor.Status = status;

        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var fornecedor = await db.Suppliers.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Supplier não encontrado", 404);
        db.Suppliers.Remove(fornecedor);
        await db.SaveChangesAsync(ct);
    }

    private static FornecedorResponse ToResponse(Supplier f) =>
        new(f.Id, f.Name, f.CnpjCpf, f.Phone, f.Email,
            f.Logradouro, f.City, f.Uf, f.Cep,
            f.ContactPerson, f.Notes, f.CreatedAt,
            f.RazaoSocial, f.NomeFantasia, f.InscricaoEstadual,
            f.WhatsApp, f.Status.ToString());
}
