using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;

/// <param name="To">Empty while the employee stays on the project.</param>
/// <returns>Id of the new team membership.</returns>
[DataContract]
public sealed record AddTeamMemberCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid EmployeeId,
    [property: DataMember(Order = 3)] Guid RoleId,
    [property: DataMember(Order = 4)] DateOnly From,
    [property: DataMember(Order = 5)] decimal HourlyRate,
    [property: DataMember(Order = 6)] DateOnly? To = null) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private AddTeamMemberCommand() : this(default, default, default, default, default, default)
    {
    }
}
