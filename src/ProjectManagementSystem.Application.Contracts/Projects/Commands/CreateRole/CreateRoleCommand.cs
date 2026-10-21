using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateRole;

/// <returns>Id of the new role.</returns>
public sealed record CreateRoleCommand(Guid ProjectId, string Name, string? Description) : ICommand<Guid>;
