using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;

/// <summary>
/// Ends the open team membership of the employee on <paramref name="To"/>.
/// </summary>
public sealed record RemoveTeamMemberCommand(Guid ProjectId, Guid EmployeeId, DateOnly To) : ICommand;
