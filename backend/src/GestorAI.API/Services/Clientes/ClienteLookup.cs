using GestorAI.API.Domain.Entities;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Clientes;

/// <summary>
/// Encontra o cliente pelo telefone ou o cadastra. Usado pelos fluxos que recebem
/// o cliente como texto livre (agendamento interno e agendamento público).
/// </summary>
public class ClienteLookup(AppDbContext db)
{
    public static string SomenteDigitos(string? telefone) =>
        new([.. (telefone ?? "").Where(char.IsDigit)]);

    /// <summary>
    /// Procura primeiro pelo telefone exato (coberto pelo índice único) e, se não achar,
    /// compara só os dígitos para não duplicar cliente gravado com outra máscara.
    /// Clientes existentes não são alterados — nem nome, nem telefone.
    /// </summary>
    public async Task<Customer> GetOrCreateAsync(
        Guid empresaId, string nome, string telefone, CancellationToken ct)
    {
        var exato = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == empresaId && c.WhatsApp == telefone, ct);
        if (exato is not null) return exato;

        var digitos = SomenteDigitos(telefone);
        if (digitos.Length > 0)
        {
            var daEmpresa = await db.Customers
                .IgnoreQueryFilters()
                .Where(c => c.CompanyId == empresaId)
                .ToListAsync(ct);

            var porDigitos = daEmpresa.FirstOrDefault(c => SomenteDigitos(c.WhatsApp) == digitos);
            if (porDigitos is not null) return porDigitos;
        }

        var cliente = new Customer
        {
            CompanyId = empresaId,
            Name = nome,
            WhatsApp = telefone,
        };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync(ct);
        return cliente;
    }
}
