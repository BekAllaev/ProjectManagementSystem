using FluentValidation;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;

public sealed class ChangeHourlyRateCommandValidator : AbstractValidator<ChangeHourlyRateCommand>
{
    public ChangeHourlyRateCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.Amount).GreaterThan(0);
    }
}
