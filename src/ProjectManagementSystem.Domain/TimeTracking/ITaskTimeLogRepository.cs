using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.TimeTracking;

/// <summary>
/// The id of a <see cref="TaskTimeLog"/> is the id of its task.
/// </summary>
public interface ITaskTimeLogRepository : IRepository<TaskTimeLog>
{
}
