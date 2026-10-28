using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Queries.GetProject;

public sealed class GetProjectQueryValidator : AbstractValidator<GetProjectQuery>
{
    public GetProjectQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
