using FluentValidation;

namespace ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;

public sealed class RenameEmployeeCommandValidator : AbstractValidator<RenameEmployeeCommand>
{
    public RenameEmployeeCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100);
    }
}
