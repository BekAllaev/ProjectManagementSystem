using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.CreateProject;

/// <param name="Currency">ISO 4217 code of the currency of all money of the project.</param>
/// <returns>Id of the new project.</returns>
public sealed record CreateProjectCommand(string Title, string? Description, string Currency) : ICommand<Guid>;
