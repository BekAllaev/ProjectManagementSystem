using FluentValidation;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(c => c.WorkPackageId).NotEmpty();
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.ResponsiblePersonId).NotEmpty();
        RuleFor(c => c.EstimatedEffort).GreaterThan(0);
    }
}
