using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeTaskResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.ChangeWorkPackageResponsible;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTask;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetTasks;
using ProjectManagementSystem.Application.WorkPackages.Queries.GetWorkPackages;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command and query of <see cref="IWorkPackageService"/> through MediatR, so the pipeline behaviors apply.
/// No logic here.
/// </summary>
[Authorize]
public sealed class WorkPackageGrpcService(ISender mediator) : IWorkPackageService
{
    public Task CancelTask(CancelTaskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task CancelWorkPackage(CancelWorkPackageCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeTaskResponsible(ChangeTaskResponsibleCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeWorkPackageResponsible(ChangeWorkPackageResponsibleCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task CompleteTask(CompleteTaskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task CompleteWorkPackage(CompleteWorkPackageCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CreatedId> CreateTask(CreateTaskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CreatedId> CreateWorkPackage(CreateWorkPackageCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<TaskDetails> GetTask(GetTaskQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task<TaskList> GetTasks(GetTasksQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task<WorkPackageList> GetWorkPackages(GetWorkPackagesQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task StartTask(StartTaskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
