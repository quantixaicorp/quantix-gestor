using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreateCompraValidator : AbstractValidator<CreateCompraRequest>
{
    private static readonly string[] CondicoesPagamento =
        ["AVista", "30d", "30_60_90d", "Parcelado", "Personalizado"];

    public CreateCompraValidator()
    {
        RuleFor(x => x.FornecedorId).NotEmpty();
        RuleFor(x => x.Data).NotEmpty();
        RuleFor(x => x.TipoCompra).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CondicaoPagamento).Must(v => CondicoesPagamento.Contains(v))
            .WithMessage("Condição de pagamento inválida.");
        RuleFor(x => x.FormaPagamento).NotEmpty();
        RuleFor(x => x.Itens).NotEmpty().WithMessage("A compra deve ter ao menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Descricao).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
            item.RuleFor(i => i.ValorUnitario).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.QtdParcelas)
            .GreaterThan(0)
            .When(x => x.CondicaoPagamento == "Parcelado")
            .WithMessage("Informe a quantidade de parcelas.");
        RuleFor(x => x.ParcelasPersonalizadas)
            .NotEmpty()
            .When(x => x.CondicaoPagamento == "Personalizado")
            .WithMessage("Informe as parcelas personalizadas.");
    }
}
