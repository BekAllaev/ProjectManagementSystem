using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;

/// <returns>Id of the new time entry.</returns>
public sealed record LogTimeCommand(
    Guid WorkPackageId,
    Guid TaskId,
    Guid EmployeeId,
    DateOnly WorkDate,
    decimal Hours) : ICommand<Guid>;
