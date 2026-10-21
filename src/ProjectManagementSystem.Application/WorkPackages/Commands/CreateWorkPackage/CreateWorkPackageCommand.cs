using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;

/// <returns>Id of the new work package.</returns>
public sealed record CreateWorkPackageCommand(Guid ProjectId, string Description, Guid ResponsiblePersonId)
    : ICommand<Guid>;
