using FluentValidation;

namespace ProjectManagementSystem.Application.Funding.Queries.GetFunding;

public sealed class GetFundingQueryValidator : AbstractValidator<GetFundingQuery>
{
    public GetFundingQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
    }
}
