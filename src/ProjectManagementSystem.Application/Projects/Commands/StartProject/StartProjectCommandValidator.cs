using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.StartProject;

public sealed class StartProjectCommandValidator : AbstractValidator<StartProjectCommand>
{
    public StartProjectCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
    }
}
