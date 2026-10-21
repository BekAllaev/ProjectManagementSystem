using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.RemoveTimeEntry;

public sealed record RemoveTimeEntryCommand(Guid TaskId, Guid EntryId) : ICommand;
