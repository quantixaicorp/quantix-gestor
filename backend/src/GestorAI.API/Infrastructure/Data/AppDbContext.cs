using GestorAI.API.Domain.Entities;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionCategory> TransactionCategories => Set<TransactionCategory>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<WeeklyAvailability> WeeklyAvailabilities => Set<WeeklyAvailability>();
    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ContractTemplate> ContractTemplates => Set<ContractTemplate>();
    public DbSet<ContractTemplateItem> ContractTemplateItems => Set<ContractTemplateItem>();
    public DbSet<AutomationLog> AutomationLogs => Set<AutomationLog>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanItem> SubscriptionPlanItems => Set<SubscriptionPlanItem>();
    public DbSet<CustomerSubscription> CustomerSubscriptions => Set<CustomerSubscription>();
    public DbSet<NicheTemplate> NicheTemplates => Set<NicheTemplate>();
    public DbSet<NicheTemplateItem> NicheTemplateItems => Set<NicheTemplateItem>();
    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();
    public DbSet<ReportLayout> ReportLayouts => Set<ReportLayout>();
    public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("gestor");

        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.CompanyId, c.WhatsApp })
            .IsUnique();

        modelBuilder.Entity<Sale>()
            .HasOne(v => v.Transaction)
            .WithOne(l => l.Sale)
            .HasForeignKey<Transaction>(l => l.SaleId);

        modelBuilder.Entity<Product>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Category>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Sale>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<TransactionCategory>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<TransactionCategory>()
            .HasIndex(c => new { c.CompanyId, c.Type, c.Name })
            .IsUnique();
        modelBuilder.Entity<Quote>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Professional>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<ScheduleBlock>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<InvoiceItem>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<CompanySettings>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Contract>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Charge>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);

        modelBuilder.Entity<Contract>()
            .HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Charge>()
            .HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanySettings>()
            .HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");

        modelBuilder.Entity<Supplier>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<AutomationLog>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Supplier>()
            .HasIndex(f => new { f.CompanyId, f.CnpjCpf })
            .IsUnique()
            .HasFilter("\"CnpjCpf\" IS NOT NULL");

        modelBuilder.Entity<ContractTemplate>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<AutomationLog>()
            .HasIndex(l => new { l.ChargeId, l.EventType });

        modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<CustomerSubscription>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);

        modelBuilder.Entity<SubscriptionPlanItem>()
            .HasOne(i => i.Plan)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CustomerSubscription>()
            .HasOne(a => a.Customer)
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerSubscription>()
            .HasOne(a => a.Plan)
            .WithMany(p => p.Subscribers)
            .HasForeignKey(a => a.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerSubscription>()
            .HasOne(a => a.Contract)
            .WithMany()
            .HasForeignKey(a => a.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NicheTemplateItem>()
            .HasOne(i => i.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.NicheTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DashboardLayout>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<DashboardLayout>()
            .HasIndex(d => d.CompanyId)
            .IsUnique();

        modelBuilder.Entity<ReportLayout>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<ReportLayout>()
            .HasIndex(r => r.CompanyId)
            .IsUnique();

        // Purchases
        modelBuilder.Entity<InstallmentPlan>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<Purchase>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<PurchaseItem>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);

        modelBuilder.Entity<Transaction>()
            .HasOne(l => l.InstallmentPlan)
            .WithMany(p => p.Installments)
            .HasForeignKey(l => l.InstallmentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasOne(c => c.Supplier)
            .WithMany()
            .HasForeignKey(c => c.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasOne(c => c.PurchaseOrder)
            .WithMany()
            .HasForeignKey(c => c.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Purchase>()
            .HasMany(c => c.Items)
            .WithOne(i => i.Purchase)
            .HasForeignKey(i => i.PurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InstallmentPlan>()
            .HasOne(p => p.Purchase)
            .WithOne(c => c.InstallmentPlan)
            .HasForeignKey<InstallmentPlan>(p => p.PurchaseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseOrder>()
            .HasMany(p => p.Items)
            .WithOne(i => i.PurchaseOrder)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
