using FluentValidation;

namespace ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;

public sealed class UpdateRiskCommandValidator : AbstractValidator<UpdateRiskCommand>
{
    public UpdateRiskCommandValidator()
    {
        RuleFor(c => c.RiskId).NotEmpty();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Probability).IsInEnum();
        RuleFor(c => c.Impact).IsInEnum();
        RuleFor(c => c.OwnerId).NotEmpty();
        RuleFor(c => c.Status).IsInEnum();
    }
}
