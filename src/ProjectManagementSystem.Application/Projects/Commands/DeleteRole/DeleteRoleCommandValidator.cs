using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.DeleteRole;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.RoleId).NotEmpty();
    }
}
