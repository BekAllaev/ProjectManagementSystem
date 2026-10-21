using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.DeleteMilestone;

public sealed class DeleteMilestoneCommandValidator : AbstractValidator<DeleteMilestoneCommand>
{
    public DeleteMilestoneCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
    }
}
