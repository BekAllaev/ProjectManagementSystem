using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryValidator : AbstractValidator<GetProjectsQuery>
{
    public GetProjectsQueryValidator()
    {
        RuleFor(q => q.State).IsInEnum();
        RuleFor(q => q.Search).MaximumLength(200);
    }
}
