using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Infrastructure.Repositories;

public class Repository<T>(AppDbContext db) where T : class
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public IQueryable<T> Query() => Set.AsQueryable();
    public async Task<T?> FindAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);
    public void Add(T entity) => Set.Add(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public async Task SaveAsync(CancellationToken ct = default) =>
        await Db.SaveChangesAsync(ct);
}
