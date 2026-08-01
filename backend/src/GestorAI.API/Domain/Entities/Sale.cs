using System.ComponentModel.DataAnnotations.Schema;
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Sale : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public StatusVenda Status { get; set; } = StatusVenda.Aberta;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public FormaPagamento PaymentMethod { get; set; }
    public int? Installments { get; set; }
    public string? Notes { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<SaleItem> Items { get; set; } = [];
    public Transaction? Transaction { get; set; }

    public Guid?   ProfessionalId   { get; set; }
    public string? ProfessionalName { get; set; }
    [Column("observacao_os")]
    public string? ServiceOrderNotes { get; set; }
}
