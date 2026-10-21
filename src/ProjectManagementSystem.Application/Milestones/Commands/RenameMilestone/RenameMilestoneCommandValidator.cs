using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.RenameMilestone;

public sealed class RenameMilestoneCommandValidator : AbstractValidator<RenameMilestoneCommand>
{
    public RenameMilestoneCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
    }
}
