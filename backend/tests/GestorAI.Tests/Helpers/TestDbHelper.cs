using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Helpers;

public static class TestDbHelper
{
    public static (AppDbContext db, TenantContext tenant) Create()
    {
        var companyId = Guid.NewGuid();
        var tenant = new TenantContext { CompanyId = companyId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            tenant);
        return (db, tenant);
    }
}
