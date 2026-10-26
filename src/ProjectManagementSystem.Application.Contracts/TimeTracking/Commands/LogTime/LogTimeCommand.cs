using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;

/// <summary>
/// Logs time of the current user; the employee is not part of the command.
/// </summary>
/// <returns>Id of the new time entry.</returns>
[DataContract]
public sealed record LogTimeCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId,
    [property: DataMember(Order = 3)] DateOnly WorkDate,
    [property: DataMember(Order = 4)] decimal Hours) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private LogTimeCommand() : this(default, default, default, default)
    {
    }
}
