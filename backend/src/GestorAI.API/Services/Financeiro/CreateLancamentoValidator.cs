using FluentValidation;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class CreateLancamentoValidator : AbstractValidator<CreateLancamentoRequest>
{
    public CreateLancamentoValidator(AppDbContext db, TenantContext tenantContext)
    {
        RuleFor(x => x.Type)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Type deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.DueDate).NotEmpty();
        RuleFor(x => x.Category)
            .NotEmpty().MaximumLength(100)
            .MustAsync(async (request, categoria, ct) =>
            {
                if (!Enum.TryParse<TipoLancamento>(request.Type, out var tipo)) return false;
                return await db.TransactionCategories
                    .AnyAsync(c => c.Name == categoria && c.Type == tipo, ct);
            })
            .WithMessage("Category não encontrada para o tipo informado.");
    }
}
