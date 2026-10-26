using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.ChangeRoleDescription;
using ProjectManagementSystem.Application.Projects.Commands.CloseProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.DeleteRole;
using ProjectManagementSystem.Application.Projects.Commands.RemoveTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.RenameRole;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command of <see cref="IProjectService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class ProjectGrpcService(ISender mediator) : IProjectService
{
    public Task<CreatedId> AddTeamMember(AddTeamMemberCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeHourlyRate(ChangeHourlyRateCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeRoleDescription(ChangeRoleDescriptionCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task CloseProject(CloseProjectCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CreatedId> CreateProject(CreateProjectCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CreatedId> CreateRole(CreateRoleCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task DeleteRole(DeleteRoleCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RemoveTeamMember(RemoveTeamMemberCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RenameRole(RenameRoleCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task StartProject(StartProjectCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
