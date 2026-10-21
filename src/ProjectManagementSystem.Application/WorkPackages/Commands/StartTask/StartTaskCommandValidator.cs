using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

public sealed class StartTaskCommandValidator : AbstractValidator<StartTaskCommand>
{
    public StartTaskCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
    }
}
