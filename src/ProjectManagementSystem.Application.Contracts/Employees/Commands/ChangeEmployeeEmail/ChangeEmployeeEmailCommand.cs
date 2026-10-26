using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;

[DataContract]
public sealed record ChangeEmployeeEmailCommand(
    [property: DataMember(Order = 1)] Guid EmployeeId,
    [property: DataMember(Order = 2)] string Email) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeEmployeeEmailCommand() : this(default, default!)
    {
    }
}
