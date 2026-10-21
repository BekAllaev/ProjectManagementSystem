using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;

public sealed class CompleteMilestoneCommandValidator : AbstractValidator<CompleteMilestoneCommand>
{
    public CompleteMilestoneCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
    }
}
