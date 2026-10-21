using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;

public sealed class CancelTaskCommandValidator : AbstractValidator<CancelTaskCommand>
{
    public CancelTaskCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
    }
}
