using FluentValidation;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;

public sealed class LogTimeCommandValidator : AbstractValidator<LogTimeCommand>
{
    public LogTimeCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.Hours).GreaterThan(0).LessThanOrEqualTo(24);
    }
}
