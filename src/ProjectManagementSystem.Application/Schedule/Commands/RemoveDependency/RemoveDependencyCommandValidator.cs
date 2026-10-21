using FluentValidation;

namespace ProjectManagementSystem.Application.Schedule.Commands.RemoveDependency;

public sealed class RemoveDependencyCommandValidator : AbstractValidator<RemoveDependencyCommand>
{
    public RemoveDependencyCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.PredecessorTaskId).NotEmpty();
        RuleFor(c => c.SuccessorTaskId).NotEmpty();
    }
}
