using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;

/// <summary>
/// Cancels the work package and all its unfinished tasks.
/// </summary>
[DataContract]
public sealed record CancelWorkPackageCommand(
    [property: DataMember(Order = 1)] Guid WorkPackageId,
    [property: DataMember(Order = 2)] string Reason) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CancelWorkPackageCommand() : this(default, default!)
    {
    }
}
