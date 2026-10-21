using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;

public sealed class ChangeRoleDescriptionCommandValidator : AbstractValidator<ChangeRoleDescriptionCommand>
{
    public ChangeRoleDescriptionCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.Description).MaximumLength(1000);
    }
}
