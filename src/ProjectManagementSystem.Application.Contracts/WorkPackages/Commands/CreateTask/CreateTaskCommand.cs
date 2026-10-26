using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;

/// <param name="EstimatedEffort">Estimated effort in hours.</param>
/// <returns>Id of the new task.</returns>
[DataContract]
public sealed record CreateTaskCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] string Name,
    [property: DataMember(Order = 3)] string? Description,
    [property: DataMember(Order = 4)] Guid ResponsiblePersonId,
    [property: DataMember(Order = 5)] decimal EstimatedEffort) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreateTaskCommand() : this(default, default!, default, default, default)
    {
    }
}
