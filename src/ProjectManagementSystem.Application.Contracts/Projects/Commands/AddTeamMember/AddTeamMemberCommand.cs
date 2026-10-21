using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;

/// <param name="To">Empty while the employee stays on the project.</param>
/// <returns>Id of the new team membership.</returns>
public sealed record AddTeamMemberCommand(
    Guid ProjectId,
    Guid EmployeeId,
    Guid RoleId,
    DateOnly From,
    decimal HourlyRate,
    DateOnly? To = null) : ICommand<Guid>;
