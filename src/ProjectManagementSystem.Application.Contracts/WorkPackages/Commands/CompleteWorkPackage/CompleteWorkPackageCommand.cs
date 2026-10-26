using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;

[DataContract]
public sealed record CompleteWorkPackageCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CompleteWorkPackageCommand() : this(Guid.Empty)
    {
    }
}
