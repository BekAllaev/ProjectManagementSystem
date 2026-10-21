using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;

public sealed class CreateWorkPackageCommandValidator : AbstractValidator<CreateWorkPackageCommand>
{
    public CreateWorkPackageCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(1000);
        RuleFor(c => c.ResponsiblePersonId).NotEmpty();
    }
}
