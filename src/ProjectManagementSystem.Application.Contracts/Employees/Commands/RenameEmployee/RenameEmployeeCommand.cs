using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;

[DataContract]
public sealed record RenameEmployeeCommand(
    [property: DataMember(Order = 1)] Guid EmployeeId,
    [property: DataMember(Order = 2)] string FirstName,
    [property: DataMember(Order = 3)] string LastName) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RenameEmployeeCommand() : this(default, default!, default!)
    {
    }
}
