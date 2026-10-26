using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;

namespace ProjectManagementSystem.Application.WorkPackages;

/// <summary>
/// gRPC service of the WorkPackage (with its tasks) aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.WorkPackageService")]
public interface IWorkPackageService
{
    Task CancelTask(CancelTaskCommand command, CallContext context = default);

    Task CancelWorkPackage(CancelWorkPackageCommand command, CallContext context = default);

    Task ChangeTaskResponsible(ChangeTaskResponsibleCommand command, CallContext context = default);

    Task ChangeWorkPackageResponsible(ChangeWorkPackageResponsibleCommand command, CallContext context = default);

    Task CompleteTask(CompleteTaskCommand command, CallContext context = default);

    Task CompleteWorkPackage(CompleteWorkPackageCommand command, CallContext context = default);

    Task<CreatedId> CreateTask(CreateTaskCommand command, CallContext context = default);

    Task<CreatedId> CreateWorkPackage(CreateWorkPackageCommand command, CallContext context = default);

    Task StartTask(StartTaskCommand command, CallContext context = default);
}
