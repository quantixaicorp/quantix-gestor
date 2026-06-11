using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Services.Assinaturas;

public class AssinaturaService(
    AppDbContext db,
    TenantContext tenantContext,
    CobrancaService cobrancaService)
{
    // Implementado na Task 6
}
