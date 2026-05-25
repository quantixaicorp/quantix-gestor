using FluentValidation;
using GestorAI.API.DTOs.Vendas;

namespace GestorAI.API.Services.Vendas;

public class CreateVendaValidator : AbstractValidator<CreateVendaRequest>
{
    public CreateVendaValidator()
    {
        RuleFor(x => x.Itens).NotEmpty().WithMessage("A venda precisa ter ao menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.ProdutoId).NotEmpty();
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
            item.RuleFor(i => i.Desconto).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Desconto).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FormaPagamento)
            .Must(f => new[] { "Dinheiro", "Pix", "Cartao", "Outro" }.Contains(f))
            .WithMessage("Forma de pagamento inválida.");
        RuleFor(x => x.Parcelas).GreaterThan(0).When(x => x.Parcelas.HasValue);
    }
}
