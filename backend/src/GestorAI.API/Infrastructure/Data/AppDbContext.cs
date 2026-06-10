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
    public DbSet<CategoriaLancamento> CategoriasLancamento => Set<CategoriaLancamento>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<OrcamentoItem> OrcamentoItens => Set<OrcamentoItem>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<DisponibilidadeSemanal> DisponibilidadeSemanais => Set<DisponibilidadeSemanal>();
    public DbSet<BloqueioAgenda> BloqueiosAgenda => Set<BloqueioAgenda>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<NotaFiscalItem> NotaFiscalItens => Set<NotaFiscalItem>();
    public DbSet<ConfiguracaoEmpresa> ConfiguracoesEmpresa => Set<ConfiguracaoEmpresa>();
    public DbSet<Contrato> Contratos => Set<Contrato>();
    public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<ContratoTemplate> ContratoTemplates => Set<ContratoTemplate>();
    public DbSet<ContratoTemplateItem> ContratoTemplateItens => Set<ContratoTemplateItem>();
    public DbSet<AutomacaoLog> AutomacaoLogs => Set<AutomacaoLog>();
    public DbSet<PlanoSaaS> PlanosSaaS => Set<PlanoSaaS>();
    public DbSet<EmpresaPlano> EmpresasPlano => Set<EmpresaPlano>();

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
        modelBuilder.Entity<CategoriaLancamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<CategoriaLancamento>()
            .HasIndex(c => new { c.EmpresaId, c.Tipo, c.Nome })
            .IsUnique();
        modelBuilder.Entity<Orcamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Profissional>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<BloqueioAgenda>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Agendamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<NotaFiscal>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<NotaFiscalItem>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<ConfiguracaoEmpresa>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Contrato>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Cobranca>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);

        modelBuilder.Entity<ContratoItem>().ToTable("ContratoItens");

        modelBuilder.Entity<Contrato>()
            .HasOne(c => c.Cliente)
            .WithMany()
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Cobranca>()
            .HasOne(c => c.Cliente)
            .WithMany()
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConfiguracaoEmpresa>()
            .HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");

        modelBuilder.Entity<Fornecedor>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<AutomacaoLog>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Fornecedor>()
            .HasIndex(f => new { f.EmpresaId, f.CnpjCpf })
            .IsUnique()
            .HasFilter("\"CnpjCpf\" IS NOT NULL");

        modelBuilder.Entity<ContratoTemplate>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<ContratoTemplateItem>().ToTable("ContratoTemplateItens");
        modelBuilder.Entity<AutomacaoLog>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<AutomacaoLog>()
            .HasIndex(l => new { l.CobrancaId, l.TipoEvento });

        var basicoId = new Guid("10000000-0000-0000-0000-000000000001");
        var profId   = new Guid("10000000-0000-0000-0000-000000000002");
        var entId    = new Guid("10000000-0000-0000-0000-000000000003");

        var criadoBasico = new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7080);
        var criadoProf   = new DateTime(2026, 6, 10, 16, 15, 2, 245, DateTimeKind.Utc).AddTicks(7810);

        modelBuilder.Entity<PlanoSaaS>().HasData(
            new PlanoSaaS
            {
                Id = basicoId,
                Nome = "Básico",
                Descricao = "Gestão essencial para pequenos negócios",
                Preco = 97m,
                Features = """["asaas_cobrancas","nota_fiscal"]""",
                CriadoEm = criadoBasico,
            },
            new PlanoSaaS
            {
                Id = profId,
                Nome = "Profissional",
                Descricao = "Automações e integrações completas",
                Preco = 197m,
                Features = """["asaas_cobrancas","nota_fiscal","automacoes_whatsapp","assinatura_digital","relatorios_avancados"]""",
                CriadoEm = criadoProf,
            },
            new PlanoSaaS
            {
                Id = entId,
                Nome = "Enterprise",
                Descricao = "Multi-profissional, sinal de reserva, tudo incluso",
                Preco = 397m,
                Features = """["asaas_cobrancas","nota_fiscal","automacoes_whatsapp","assinatura_digital","relatorios_avancados","sinal_reserva","multi_profissional"]""",
                CriadoEm = criadoProf,
            }
        );
    }
}
