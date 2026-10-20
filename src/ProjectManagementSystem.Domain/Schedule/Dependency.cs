namespace ProjectManagementSystem.Domain.Schedule;

public sealed record Dependency(
    Guid PredecessorTaskId,
    Guid PredecessorWorkPackageId,
    Guid SuccessorTaskId,
    Guid SuccessorWorkPackageId,
    DependencyType Type);
