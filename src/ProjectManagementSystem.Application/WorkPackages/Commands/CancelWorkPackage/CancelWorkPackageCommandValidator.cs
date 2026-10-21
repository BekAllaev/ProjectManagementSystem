using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;

public sealed class CancelWorkPackageCommandValidator : AbstractValidator<CancelWorkPackageCommand>
{
    public CancelWorkPackageCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(1000);
    }
}
