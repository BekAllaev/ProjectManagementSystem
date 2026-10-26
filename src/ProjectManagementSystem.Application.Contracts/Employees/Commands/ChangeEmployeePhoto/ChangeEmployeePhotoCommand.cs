using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;

/// <param name="PhotoPath">Empty removes the photo.</param>
[DataContract]
public sealed record ChangeEmployeePhotoCommand(
    [property: DataMember(Order = 1)] Guid EmployeeId,
    [property: DataMember(Order = 2)] string? PhotoPath) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeEmployeePhotoCommand() : this(default, default)
    {
    }
}
