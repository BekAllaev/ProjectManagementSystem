using FluentValidation;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;

public sealed class RemoveTimeEntryCommandValidator : AbstractValidator<RemoveTimeEntryCommand>
{
    public RemoveTimeEntryCommandValidator()
    {
        RuleFor(c => c.TaskId).NotEmpty();
        RuleFor(c => c.EntryId).NotEmpty();
    }
}
