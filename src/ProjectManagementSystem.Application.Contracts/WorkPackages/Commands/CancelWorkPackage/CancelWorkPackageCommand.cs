using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;

/// <summary>
/// Cancels the work package and all its unfinished tasks.
/// </summary>
public sealed record CancelWorkPackageCommand(Guid WorkPackageId, string Reason) : ICommand;
