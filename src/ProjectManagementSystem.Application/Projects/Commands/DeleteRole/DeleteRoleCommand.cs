using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid ProjectId, Guid RoleId) : ICommand;
