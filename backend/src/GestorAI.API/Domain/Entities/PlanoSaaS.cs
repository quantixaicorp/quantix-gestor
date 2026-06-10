namespace GestorAI.API.Domain.Entities;

public class PlanoSaaS
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }
    public required string Descricao { get; set; }
    public decimal Preco { get; set; }
    public required string Features { get; set; } // JSON array: ["asaas_cobrancas","automacoes_whatsapp"]
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<EmpresaPlano> EmpresasPlano { get; set; } = [];
}
