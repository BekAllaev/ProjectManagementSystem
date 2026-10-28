using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
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
using ProjectManagementSystem.Application.Projects.Queries.GetProject;
using ProjectManagementSystem.Application.Projects.Queries.GetProjects;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Application.Projects.Queries.GetTeam;

namespace ProjectManagementSystem.Application.Projects;

/// <summary>
/// gRPC service of the Project (with its roles and team) aggregate: one method per command and per query. The host
/// implements every method by sending the command or query through MediatR; the client shares this interface for typed
/// calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.ProjectService")]
public interface IProjectService
{
    Task<CreatedId> AddTeamMember(AddTeamMemberCommand command, CallContext context = default);

    Task ChangeHourlyRate(ChangeHourlyRateCommand command, CallContext context = default);

    Task ChangeRoleDescription(ChangeRoleDescriptionCommand command, CallContext context = default);

    Task CloseProject(CloseProjectCommand command, CallContext context = default);

    Task<CreatedId> CreateProject(CreateProjectCommand command, CallContext context = default);

    Task<CreatedId> CreateRole(CreateRoleCommand command, CallContext context = default);

    Task DeleteRole(DeleteRoleCommand command, CallContext context = default);

    Task<ProjectDetails> GetProject(GetProjectQuery query, CallContext context = default);

    Task<ProjectList> GetProjects(GetProjectsQuery query, CallContext context = default);

    Task<RoleList> GetRoles(GetRolesQuery query, CallContext context = default);

    Task<Team> GetTeam(GetTeamQuery query, CallContext context = default);

    Task RemoveTeamMember(RemoveTeamMemberCommand command, CallContext context = default);

    Task RenameRole(RenameRoleCommand command, CallContext context = default);

    Task StartProject(StartProjectCommand command, CallContext context = default);
}
