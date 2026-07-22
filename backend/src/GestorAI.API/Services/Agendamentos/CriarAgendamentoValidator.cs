using FluentValidation;
using GestorAI.API.DTOs.Agendamentos;

namespace GestorAI.API.Services.Agendamentos;

public class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoRequest>
{
    public CriarAgendamentoValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.StartAt).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("StartAt deve ser no futuro.");
    }
}
