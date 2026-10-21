using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;

public sealed record ChangeRoleDescriptionCommand(Guid ProjectId, Guid RoleId, string? Description) : ICommand;
