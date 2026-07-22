using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Clientes;

public class ClienteService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ClienteResponse>> ListAsync(string? busca, CancellationToken ct)
    {
        var query = db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(c => c.Name.Contains(busca) || c.WhatsApp.Contains(busca));

        return await query
            .OrderBy(c => c.Name)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<ClienteResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Customers.FindAsync([id], ct)
            ?? throw new AppException("Customer não encontrado", 404);
        return ToResponse(c);
    }

    public async Task<ClienteResponse> CreateAsync(CreateClienteRequest req, CancellationToken ct)
    {
        var cliente = new Customer
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            WhatsApp = req.WhatsApp,
            Email = req.Email,
            Notes = req.Notes,
        };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> UpdateAsync(Guid id, UpdateClienteRequest req, CancellationToken ct)
    {
        var cliente = await db.Customers.FindAsync([id], ct)
            ?? throw new AppException("Customer não encontrado", 404);

        cliente.Name = req.Name;
        cliente.WhatsApp = req.WhatsApp;
        cliente.Email = req.Email;
        cliente.Notes = req.Notes;

        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var cliente = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Customer não encontrado.", 404);

        var temVinculos =
            await db.Contracts.AnyAsync(c => c.CustomerId == id, ct) ||
            await db.Charges.AnyAsync(c => c.CustomerId == id, ct) ||
            await db.Quotes.AnyAsync(o => o.CustomerId == id, ct) ||
            await db.Sales.AnyAsync(v => v.CustomerId == id, ct);

        if (temVinculos)
            throw new AppException(
                "Este cliente possui dados vinculados (contratos, cobranças, orçamentos ou vendas) e não pode ser excluído.", 400);

        db.Customers.Remove(cliente);
        await db.SaveChangesAsync(ct);
    }

    private static ClienteResponse ToResponse(Customer c) =>
        new(c.Id, c.Name, c.WhatsApp, c.Email, c.Notes, c.CreatedAt);
}
