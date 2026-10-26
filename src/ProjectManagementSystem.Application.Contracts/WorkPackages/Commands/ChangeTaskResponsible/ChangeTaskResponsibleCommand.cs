using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;

[DataContract]
public sealed record ChangeTaskResponsibleCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid TaskId,
    [property: DataMember(Order = 3)] Guid EmployeeId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeTaskResponsibleCommand() : this(default, default, default)
    {
    }
}
