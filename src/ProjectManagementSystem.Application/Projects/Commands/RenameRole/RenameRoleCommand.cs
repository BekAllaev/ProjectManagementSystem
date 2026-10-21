using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.RenameRole;

public sealed record RenameRoleCommand(Guid ProjectId, Guid RoleId, string NewName) : ICommand;
