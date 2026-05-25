using FluentValidation;
using GestorAI.API.DTOs.Estoque;

namespace GestorAI.API.Services.Estoque;

public class CreateProdutoValidator : AbstractValidator<CreateProdutoRequest>
{
    public CreateProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrecoVenda).GreaterThan(0);
        RuleFor(x => x.CustoMedio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstoqueAtual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstoqueMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoriaId).NotEmpty();
    }
}

public class EntradaEstoqueValidator : AbstractValidator<EntradaEstoqueRequest>
{
    public EntradaEstoqueValidator()
    {
        RuleFor(x => x.ProdutoId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
        RuleFor(x => x.CustoUnitario).GreaterThanOrEqualTo(0).When(x => x.CustoUnitario.HasValue);
    }
}
