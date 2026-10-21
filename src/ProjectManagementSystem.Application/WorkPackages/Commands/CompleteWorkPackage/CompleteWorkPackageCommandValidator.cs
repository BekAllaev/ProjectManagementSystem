using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;

public sealed class CompleteWorkPackageCommandValidator : AbstractValidator<CompleteWorkPackageCommand>
{
    public CompleteWorkPackageCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
    }
}
