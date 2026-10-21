using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Schedule;

namespace ProjectManagementSystem.Application.Schedule.Commands.AddDependency;

/// <summary>
/// Makes the successor task depend on the predecessor task. Each task is given with its work package.
/// </summary>
public sealed record AddDependencyCommand(
    Guid ProjectId,
    Guid PredecessorWorkPackageId,
    Guid PredecessorTaskId,
    Guid SuccessorWorkPackageId,
    Guid SuccessorTaskId,
    DependencyType Type) : ICommand;
