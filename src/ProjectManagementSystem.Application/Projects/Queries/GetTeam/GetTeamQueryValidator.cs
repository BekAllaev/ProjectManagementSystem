using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Queries.GetTeam;

public sealed class GetTeamQueryValidator : AbstractValidator<GetTeamQuery>
{
    public GetTeamQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
