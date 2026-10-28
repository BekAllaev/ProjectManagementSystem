using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;

public sealed class GetTasksQueryValidator : AbstractValidator<GetTasksQuery>
{
    public GetTasksQueryValidator()
    {
        RuleFor(q => q.ProjectId).NotEmpty();
        RuleFor(q => q.Status).IsInEnum();
    }
}
