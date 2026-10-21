using FluentValidation;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;

public sealed class ChangeEmployeeEmailCommandValidator : AbstractValidator<ChangeEmployeeEmailCommand>
{
    public ChangeEmployeeEmailCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Email).NotEmpty().MaximumLength(254).EmailAddress();
    }
}
