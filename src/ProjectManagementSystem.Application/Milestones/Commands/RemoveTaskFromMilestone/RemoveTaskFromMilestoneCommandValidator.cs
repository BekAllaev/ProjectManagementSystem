using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.RemoveTaskFromMilestone;

public sealed class RemoveTaskFromMilestoneCommandValidator : AbstractValidator<RemoveTaskFromMilestoneCommand>
{
    public RemoveTaskFromMilestoneCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
    }
}
