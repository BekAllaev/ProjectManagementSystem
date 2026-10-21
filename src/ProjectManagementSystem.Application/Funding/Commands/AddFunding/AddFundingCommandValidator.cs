using FluentValidation;

namespace ProjectManagementSystem.Application.Funding.Commands.AddFunding;

public sealed class AddFundingCommandValidator : AbstractValidator<AddFundingCommand>
{
    public AddFundingCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
        RuleFor(c => c.Reason).MaximumLength(1000);
    }
}
