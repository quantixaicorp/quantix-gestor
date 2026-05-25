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
        var query = db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(c => c.Nome.Contains(busca) || c.Whatsapp.Contains(busca));

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<ClienteResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Clientes.FindAsync([id], ct)
            ?? throw new AppException("Cliente não encontrado", 404);
        return ToResponse(c);
    }

    public async Task<ClienteResponse> CreateAsync(CreateClienteRequest req, CancellationToken ct)
    {
        var cliente = new Cliente
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Whatsapp = req.Whatsapp,
            Email = req.Email,
            Observacoes = req.Observacoes,
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> UpdateAsync(Guid id, UpdateClienteRequest req, CancellationToken ct)
    {
        var cliente = await db.Clientes.FindAsync([id], ct)
            ?? throw new AppException("Cliente não encontrado", 404);

        cliente.Nome = req.Nome;
        cliente.Whatsapp = req.Whatsapp;
        cliente.Email = req.Email;
        cliente.Observacoes = req.Observacoes;

        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    private static ClienteResponse ToResponse(Cliente c) =>
        new(c.Id, c.Nome, c.Whatsapp, c.Email, c.Observacoes, c.DataCadastro);
}
