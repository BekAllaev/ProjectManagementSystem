using FluentValidation;

namespace ProjectManagementSystem.Application.Schedule.Queries.GetDependencies;

public sealed class GetDependenciesQueryValidator : AbstractValidator<GetDependenciesQuery>
{
    public GetDependenciesQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
