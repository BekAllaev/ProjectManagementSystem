using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;

public sealed class GetTaskQueryValidator : AbstractValidator<GetTaskQuery>
{
    public GetTaskQueryValidator()
    {
        RuleFor(q => q.WorkPackageId).NotEmpty();
        RuleFor(q => q.TaskId).NotEmpty();
    }
}
