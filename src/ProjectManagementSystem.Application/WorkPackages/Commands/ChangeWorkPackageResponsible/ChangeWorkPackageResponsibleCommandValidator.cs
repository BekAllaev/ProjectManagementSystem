using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;

public sealed class ChangeWorkPackageResponsibleCommandValidator : AbstractValidator<ChangeWorkPackageResponsibleCommand>
{
    public ChangeWorkPackageResponsibleCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
    }
}
