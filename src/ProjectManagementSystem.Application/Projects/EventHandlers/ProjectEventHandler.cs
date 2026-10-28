using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Application.Projects.EventHandlers;

/// <summary>
/// Reacts to the events of the Project aggregate. It keeps the read models of the project, its roles and its team
/// (every event sets the version of the project), and when a membership ends it re-checks that the responsible persons
/// of open work are still on the team. The read models only get values set, so a repeated delivery changes nothing;
/// an event of a project without a read model (saved before the read models existed) changes no read model.
/// </summary>
public sealed class ProjectEventHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<WorkPackageItemView> workPackages,
    IReadModelRepository<RoleItemView> roles,
    IReadModelRepository<TeamMemberItemView> teamMembers,
    IReadModelRepository<MembershipPeriodView> periods,
    IReadModelRepository<HourlyRateItemView> rates,
    ResponsiblePersonCheck responsiblePersonCheck) :
    IDomainEventHandler<ProjectCreated>,
    IDomainEventHandler<ProjectStarted>,
    IDomainEventHandler<ProjectCompleted>,
    IDomainEventHandler<WorkPackageRegistered>,
    IDomainEventHandler<RoleCreated>,
    IDomainEventHandler<RoleRenamed>,
    IDomainEventHandler<RoleDescriptionChanged>,
    IDomainEventHandler<RoleDeleted>,
    IDomainEventHandler<TeamMemberAdded>,
    IDomainEventHandler<TeamMembershipEnded>,
    IDomainEventHandler<HourlyRateChanged>
{
    public async Task HandleAsync(ProjectCreated domainEvent, CancellationToken cancellationToken = default)
    {
        var project = await projects.FindAsync(p => p.Id == domainEvent.ProjectId, cancellationToken);
        if (project is null)
        {
            project = new ProjectItemView { Id = domainEvent.ProjectId, State = ProjectState.Created, IsOpen = true };
            projects.Add(project);
        }

        project.Title = domainEvent.Title;
        project.Description = domainEvent.Description ?? string.Empty;
        project.Currency = domainEvent.Currency;
        project.CreationDate = domainEvent.CreationDate;
        project.Version = Math.Max(project.Version, domainEvent.AggregateVersion);
    }

    public Task HandleAsync(ProjectStarted domainEvent, CancellationToken cancellationToken = default) =>
        ChangeProjectAsync(domainEvent.ProjectId, domainEvent, project =>
        {
            project.State = ProjectState.InProgress;
            project.StartDate = domainEvent.StartDate;
        }, cancellationToken);

    public Task HandleAsync(ProjectCompleted domainEvent, CancellationToken cancellationToken = default) =>
        ChangeProjectAsync(domainEvent.ProjectId, domainEvent, project =>
        {
            project.State = ProjectState.Completed;
            project.IsOpen = false;
            project.CompletionDate = domainEvent.CompletionDate;
        }, cancellationToken);

    public async Task HandleAsync(WorkPackageRegistered domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        var workPackage = await workPackages.FindAsync(w => w.Id == domainEvent.WorkPackageId, cancellationToken);
        if (workPackage is not null && !workPackage.IsRegistered)
        {
            var registered = await workPackages.ListAsync(w => w.ProjectId == domainEvent.ProjectId && w.IsRegistered, cancellationToken);
            workPackage.IsRegistered = true;
            workPackage.RegistrationPosition = registered.Count + 1;
        }
    }

    public async Task HandleAsync(RoleCreated domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        var role = await roles.FindAsync(r => r.Id == domainEvent.RoleId, cancellationToken);
        if (role is null)
        {
            role = new RoleItemView { Id = domainEvent.RoleId, ProjectId = domainEvent.ProjectId };
            roles.Add(role);
        }

        role.Name = domainEvent.Name;
        role.Description = domainEvent.Description ?? string.Empty;
    }

    public async Task HandleAsync(RoleRenamed domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await roles.FindAsync(r => r.Id == domainEvent.RoleId, cancellationToken) is { } role)
            role.Name = domainEvent.NewName;

        // A role is renamed only while it was never assigned, so no team member shows it yet; kept in step anyway.
        foreach (var member in await teamMembers.ListAsync(m => m.RoleId == domainEvent.RoleId, cancellationToken))
            member.RoleName = domainEvent.NewName;
    }

    public async Task HandleAsync(RoleDescriptionChanged domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await roles.FindAsync(r => r.Id == domainEvent.RoleId, cancellationToken) is { } role)
            role.Description = domainEvent.Description ?? string.Empty;
    }

    public async Task HandleAsync(RoleDeleted domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await roles.FindAsync(r => r.Id == domainEvent.RoleId, cancellationToken) is { } role)
            roles.Remove(role);
    }

    public async Task HandleAsync(TeamMemberAdded domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await periods.FindAsync(p => p.TeamMemberId == domainEvent.TeamMemberId, cancellationToken) is null)
        {
            periods.Add(new MembershipPeriodView
            {
                TeamMemberId = domainEvent.TeamMemberId,
                ProjectId = domainEvent.ProjectId,
                EmployeeId = domainEvent.EmployeeId,
                From = domainEvent.From,
                To = domainEvent.To,
            });
        }

        await AddRateAsync(domainEvent.ProjectId, domainEvent.TeamMemberId, domainEvent.EmployeeId, domainEvent.From, domainEvent.HourlyRate, cancellationToken);

        var role = await roles.FindAsync(r => r.Id == domainEvent.RoleId, cancellationToken);
        if (role is not null)
            role.IsAssigned = true;

        var member = await teamMembers.FindAsync(
            m => m.ProjectId == domainEvent.ProjectId && m.EmployeeId == domainEvent.EmployeeId, cancellationToken);
        if (member is null)
        {
            member = new TeamMemberItemView { ProjectId = domainEvent.ProjectId, EmployeeId = domainEvent.EmployeeId };
            teamMembers.Add(member);
        }

        // The member shows the role of the latest period.
        var memberPeriods = await periods.ListAsync(
            p => p.ProjectId == domainEvent.ProjectId && p.EmployeeId == domainEvent.EmployeeId, cancellationToken);
        if (memberPeriods.MaxBy(p => p.From)?.TeamMemberId == domainEvent.TeamMemberId)
        {
            member.RoleId = domainEvent.RoleId;
            member.RoleName = role?.Name ?? string.Empty;
        }
    }

    public async Task HandleAsync(TeamMembershipEnded domainEvent, CancellationToken cancellationToken = default)
    {
        await ProjectAsync(domainEvent, cancellationToken);
        await responsiblePersonCheck.CheckLeavingAsync(domainEvent.ProjectId, domainEvent.EmployeeId, cancellationToken);
    }

    /// <summary>
    /// Only the change of the read models, without the re-check; also used to rebuild the read models.
    /// </summary>
    public async Task ProjectAsync(TeamMembershipEnded domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await periods.FindAsync(p => p.TeamMemberId == domainEvent.TeamMemberId, cancellationToken) is { } period)
            period.To = domainEvent.To;
    }

    public async Task HandleAsync(HourlyRateChanged domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        await AddRateAsync(domainEvent.ProjectId, domainEvent.TeamMemberId, domainEvent.EmployeeId, domainEvent.From, domainEvent.Amount, cancellationToken);
    }

    private async Task ChangeProjectAsync(
        Guid projectId, IDomainEvent domainEvent, Action<ProjectItemView> change, CancellationToken cancellationToken)
    {
        var project = await projects.FindAsync(p => p.Id == projectId, cancellationToken);
        if (project is null)
            return;

        change(project);
        project.Version = Math.Max(project.Version, domainEvent.AggregateVersion);
    }

    private async Task AddRateAsync(
        Guid projectId, Guid teamMemberId, Guid employeeId, DateOnly from, decimal amount, CancellationToken cancellationToken)
    {
        if (await rates.FindAsync(r => r.TeamMemberId == teamMemberId && r.From == from, cancellationToken) is not null)
            return;

        rates.Add(new HourlyRateItemView { TeamMemberId = teamMemberId, From = from, ProjectId = projectId, EmployeeId = employeeId, Amount = amount });
    }
}
