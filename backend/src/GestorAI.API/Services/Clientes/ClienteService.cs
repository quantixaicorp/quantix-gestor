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
            query = query.Where(c =>
                c.Name.Contains(busca) ||
                c.WhatsApp.Contains(busca) ||
                (c.CnpjCpf != null && c.CnpjCpf.Contains(busca)));

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
        var (personType, cnpjCpf) = ClienteDocumento.Normalize(req.PersonType, req.CnpjCpf);
        var cliente = new Customer
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            WhatsApp = req.WhatsApp,
            Email = req.Email,
            PersonType = personType,
            CnpjCpf = cnpjCpf,
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

        var (personType, cnpjCpf) = ClienteDocumento.Normalize(req.PersonType, req.CnpjCpf);
        cliente.Name = req.Name;
        cliente.WhatsApp = req.WhatsApp;
        cliente.Email = req.Email;
        cliente.PersonType = personType;
        cliente.CnpjCpf = cnpjCpf;
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
            await db.Sales.AnyAsync(v => v.CustomerId == id, ct) ||
            await db.CustomerSubscriptions.AnyAsync(a => a.CustomerId == id, ct);

        if (temVinculos)
            throw new AppException(
                "Este cliente possui dados vinculados (contratos, cobranças, orçamentos, vendas ou assinaturas) e não pode ser excluído.", 400);

        // Agendamento guarda nome e telefone na própria linha e o vínculo é opcional,
        // então desvinculamos em vez de bloquear — é o ClientSetNull que o modelo já
        // declara, que o EF só aplica a entidades carregadas.
        var agendamentos = await db.Appointments
            .Where(a => a.CustomerId == id)
            .ToListAsync(ct);
        foreach (var agendamento in agendamentos)
            agendamento.CustomerId = null;

        db.Customers.Remove(cliente);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Rede de segurança: qualquer vínculo não previsto acima viraria 500.
            throw new AppException(
                "Este cliente possui dados vinculados e não pode ser excluído.", 400);
        }
    }

    private static ClienteResponse ToResponse(Customer c) =>
        new(c.Id, c.Name, c.WhatsApp, c.Email, c.Notes, c.CreatedAt, c.PersonType, c.CnpjCpf);
}
