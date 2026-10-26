using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;

/// <returns>Id of the new employee.</returns>
[DataContract]
public sealed record RegisterEmployeeCommand(
    [property: DataMember(Order = 1)] string FirstName,
    [property: DataMember(Order = 2)] string LastName,
    [property: DataMember(Order = 3)] string Email,
    [property: DataMember(Order = 4)] string? PhotoPath = null) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RegisterEmployeeCommand() : this(default!, default!, default!, default)
    {
    }
}
