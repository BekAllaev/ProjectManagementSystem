using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.RenameRole;

public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand>
{
    public RenameRoleCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.NewName).NotEmpty().MaximumLength(100);
    }
}
