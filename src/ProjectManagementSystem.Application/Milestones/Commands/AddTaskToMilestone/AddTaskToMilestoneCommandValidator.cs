using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;

public sealed class AddTaskToMilestoneCommandValidator : AbstractValidator<AddTaskToMilestoneCommand>
{
    public AddTaskToMilestoneCommandValidator()
    {
        RuleFor(c => c.MilestoneId).NotEmpty();
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
    }
}
