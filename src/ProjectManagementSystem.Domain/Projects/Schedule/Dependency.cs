namespace ProjectManagementSystem.Domain.Projects.Schedule;

public sealed record Dependency(
    Guid PredecessorTaskId,
    Guid PredecessorWorkPackageId,
    Guid SuccessorTaskId,
    Guid SuccessorWorkPackageId,
    DependencyType Type);
