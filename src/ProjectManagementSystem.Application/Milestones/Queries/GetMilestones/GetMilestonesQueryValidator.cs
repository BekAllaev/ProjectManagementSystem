using FluentValidation;

namespace ProjectManagementSystem.Application.Milestones.Queries.GetMilestones;

public sealed class GetMilestonesQueryValidator : AbstractValidator<GetMilestonesQuery>
{
    public GetMilestonesQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
