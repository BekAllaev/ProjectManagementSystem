using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;

/// <summary>
/// Logs time of the current user; the employee is not part of the command.
/// </summary>
/// <returns>Id of the new time entry.</returns>
public sealed record LogTimeCommand(
    Guid WorkPackageId,
    Guid TaskId,
    DateOnly WorkDate,
    decimal Hours) : ICommand<Guid>;
