using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreatePedidoCompraValidator : AbstractValidator<CreatePedidoCompraRequest>
{
    public CreatePedidoCompraValidator()
    {
        RuleFor(x => x.FornecedorId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.Itens).NotEmpty().WithMessage("O pedido deve ter ao menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Descricao).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
            item.RuleFor(i => i.ValorEstimado).GreaterThanOrEqualTo(0);
        });
    }
}
