using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Queries.GetRoles;

public sealed class GetRolesQueryValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
