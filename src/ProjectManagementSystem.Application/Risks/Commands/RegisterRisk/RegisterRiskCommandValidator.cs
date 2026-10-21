using FluentValidation;

namespace ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;

public sealed class RegisterRiskCommandValidator : AbstractValidator<RegisterRiskCommand>
{
    public RegisterRiskCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Probability).IsInEnum();
        RuleFor(c => c.Impact).IsInEnum();
        RuleFor(c => c.OwnerId).NotEmpty();
    }
}
