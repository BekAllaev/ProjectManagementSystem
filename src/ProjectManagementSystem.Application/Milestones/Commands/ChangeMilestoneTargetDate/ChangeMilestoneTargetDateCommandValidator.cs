using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.ChangeMilestoneTargetDate;

public sealed class ChangeMilestoneTargetDateCommandValidator : AbstractValidator<ChangeMilestoneTargetDateCommand>
{
    public ChangeMilestoneTargetDateCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
    }
}
