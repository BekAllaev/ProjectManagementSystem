using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;

public sealed class GetWorkPackagesQueryValidator : AbstractValidator<GetWorkPackagesQuery>
{
    public GetWorkPackagesQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
