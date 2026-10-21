using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;

/// <param name="EstimatedEffort">Estimated effort in hours.</param>
/// <returns>Id of the new task.</returns>
public sealed record CreateTaskCommand(
    Guid WorkPackageId,
    string Name,
    string? Description,
    Guid ResponsiblePersonId,
    decimal EstimatedEffort) : ICommand<Guid>;
