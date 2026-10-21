using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;

public sealed class AddTeamMemberCommandValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.HourlyRate).GreaterThan(0);
    }
}
