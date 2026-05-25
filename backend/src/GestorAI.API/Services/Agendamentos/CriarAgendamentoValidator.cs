using FluentValidation;
using GestorAI.API.DTOs.Agendamentos;

namespace GestorAI.API.Services.Agendamentos;

public class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoRequest>
{
    public CriarAgendamentoValidator()
    {
        RuleFor(x => x.ProfissionalId).NotEmpty();
        RuleFor(x => x.ClienteNome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClienteTelefone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ServicoId).NotEmpty();
        RuleFor(x => x.DataHoraInicio).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("DataHoraInicio deve ser no futuro.");
    }
}
