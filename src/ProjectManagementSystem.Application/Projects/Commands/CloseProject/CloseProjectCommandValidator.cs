using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.CloseProject;

public sealed class CloseProjectCommandValidator : AbstractValidator<CloseProjectCommand>
{
    public CloseProjectCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
    }
}
