using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;

/// <summary>
/// Ends the open team membership of the employee on <paramref name="To"/>.
/// </summary>
[DataContract]
public sealed record RemoveTeamMemberCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid EmployeeId,
    [property: DataMember(Order = 3)] DateOnly To) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RemoveTeamMemberCommand() : this(default, default, default)
    {
    }
}
