// backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<OrcamentoItem> OrcamentoItens => Set<OrcamentoItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.EmpresaId, c.Whatsapp })
            .IsUnique();

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Lancamento)
            .WithOne(l => l.Venda)
            .HasForeignKey<Lancamento>(l => l.VendaId);

        modelBuilder.Entity<Produto>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Categoria>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<MovimentacaoEstoque>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Venda>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Lancamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Orcamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
    }
}
