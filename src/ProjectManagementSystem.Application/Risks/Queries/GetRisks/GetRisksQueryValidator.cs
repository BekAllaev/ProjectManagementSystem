using FluentValidation;

namespace ProjectManagementSystem.Application.Risks.Queries.GetRisks;

public sealed class GetRisksQueryValidator : AbstractValidator<GetRisksQuery>
{
    public GetRisksQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
        RuleFor(q => q.Status).IsInEnum();
    }
}
