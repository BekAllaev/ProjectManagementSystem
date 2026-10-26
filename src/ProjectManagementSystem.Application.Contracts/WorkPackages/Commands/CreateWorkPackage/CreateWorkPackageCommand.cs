using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;

/// <returns>Id of the new work package.</returns>
[DataContract]
public sealed record CreateWorkPackageCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] Guid ResponsiblePersonId) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CreateWorkPackageCommand() : this(default, default!, default)
    {
    }
}
