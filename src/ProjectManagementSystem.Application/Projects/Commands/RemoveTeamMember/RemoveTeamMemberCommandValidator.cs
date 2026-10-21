using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;

public sealed class RemoveTeamMemberCommandValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
    }
}
