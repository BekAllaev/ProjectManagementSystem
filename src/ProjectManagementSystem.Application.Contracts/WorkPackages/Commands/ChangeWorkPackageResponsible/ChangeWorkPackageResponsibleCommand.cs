using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;

[DataContract]
public sealed record ChangeWorkPackageResponsibleCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] Guid EmployeeId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeWorkPackageResponsibleCommand() : this(default, default)
    {
    }
}
