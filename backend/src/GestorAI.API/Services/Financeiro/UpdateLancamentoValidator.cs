using FluentValidation;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class UpdateLancamentoValidator : AbstractValidator<UpdateLancamentoRequest>
{
    public UpdateLancamentoValidator(AppDbContext db, TenantContext tenantContext)
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Tipo deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.DataVencimento).NotEmpty();
        RuleFor(x => x.Categoria)
            .NotEmpty().MaximumLength(100)
            .MustAsync(async (request, categoria, ct) =>
            {
                if (!Enum.TryParse<TipoLancamento>(request.Tipo, out var tipo)) return false;
                return await db.CategoriasLancamento
                    .AnyAsync(c => c.Nome == categoria && c.Tipo == tipo, ct);
            })
            .WithMessage("Categoria não encontrada para o tipo informado.");
    }
}
