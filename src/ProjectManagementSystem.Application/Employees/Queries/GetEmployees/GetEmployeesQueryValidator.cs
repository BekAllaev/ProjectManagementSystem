using FluentValidation;

namespace ProjectManagementSystem.Application.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(q => q.Search).MaximumLength(200);
    }
}
