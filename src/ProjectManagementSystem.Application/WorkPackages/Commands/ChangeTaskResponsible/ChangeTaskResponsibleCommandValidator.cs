using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;

public sealed class ChangeTaskResponsibleCommandValidator : AbstractValidator<ChangeTaskResponsibleCommand>
{
    public ChangeTaskResponsibleCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
    }
}
