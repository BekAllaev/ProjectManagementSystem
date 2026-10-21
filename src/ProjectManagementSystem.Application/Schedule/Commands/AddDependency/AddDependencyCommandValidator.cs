using FluentValidation;

namespace ProjectManagementSystem.Application.Schedule.Commands.AddDependency;

public sealed class AddDependencyCommandValidator : AbstractValidator<AddDependencyCommand>
{
    public AddDependencyCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.PredecessorWorkPackageId).NotEmpty();
        RuleFor(c => c.PredecessorTaskId).NotEmpty();
        RuleFor(c => c.SuccessorWorkPackageId).NotEmpty();
        RuleFor(c => c.SuccessorTaskId).NotEmpty();
        RuleFor(c => c.Type).IsInEnum();
    }
}
