using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Projects.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the read models of the Project aggregate: the row of the project, its roles and its team. Every event sets
/// the version of the project. A projection only sets values, so a repeated delivery changes nothing; an event of a
/// project whose row does not exist (saved before the read models existed) is ignored.
/// </summary>
internal sealed class ProjectProjection(ReadModelStore store) :
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
        var project = await store.FindAsync<ProjectItemView>(cancellationToken, domainEvent.ProjectId);
        if (project is null)
        {
            project = new ProjectItemView { Id = domainEvent.ProjectId, State = ProjectState.Created, IsOpen = true };
            store.Add(project);
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

        var workPackage = await store.FindAsync<WorkPackageItemView>(cancellationToken, domainEvent.WorkPackageId);
        if (workPackage is not null && !workPackage.IsRegistered)
        {
            workPackage.IsRegistered = true;
            workPackage.RegisteredAt = domainEvent.OccurredAt;
        }
    }

    public async Task HandleAsync(RoleCreated domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        var role = await store.FindAsync<RoleItemView>(cancellationToken, domainEvent.RoleId);
        if (role is null)
        {
            role = new RoleItemView { Id = domainEvent.RoleId, ProjectId = domainEvent.ProjectId };
            store.Add(role);
        }

        role.Name = domainEvent.Name;
        role.Description = domainEvent.Description ?? string.Empty;
    }

    public async Task HandleAsync(RoleRenamed domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await store.FindAsync<RoleItemView>(cancellationToken, domainEvent.RoleId) is { } role)
            role.Name = domainEvent.NewName;

        // A role is renamed only while it was never assigned, so no team member shows it yet; kept in step anyway.
        foreach (var member in await store.LoadAsync<TeamMemberItemView>(m => m.RoleId == domainEvent.RoleId, cancellationToken))
            member.RoleName = domainEvent.NewName;
    }

    public async Task HandleAsync(RoleDescriptionChanged domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await store.FindAsync<RoleItemView>(cancellationToken, domainEvent.RoleId) is { } role)
            role.Description = domainEvent.Description ?? string.Empty;
    }

    public async Task HandleAsync(RoleDeleted domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await store.FindAsync<RoleItemView>(cancellationToken, domainEvent.RoleId) is { } role)
            store.Remove(role);
    }

    public async Task HandleAsync(TeamMemberAdded domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await store.FindAsync<MembershipPeriodView>(cancellationToken, domainEvent.TeamMemberId) is null)
        {
            store.Add(new MembershipPeriodView
            {
                TeamMemberId = domainEvent.TeamMemberId,
                ProjectId = domainEvent.ProjectId,
                EmployeeId = domainEvent.EmployeeId,
                From = domainEvent.From,
                To = domainEvent.To,
            });
        }

        await AddRateAsync(domainEvent.ProjectId, domainEvent.TeamMemberId, domainEvent.EmployeeId, domainEvent.From, domainEvent.HourlyRate, cancellationToken);

        var role = await store.FindAsync<RoleItemView>(cancellationToken, domainEvent.RoleId);
        if (role is not null)
            role.IsAssigned = true;

        var member = await store.FindAsync<TeamMemberItemView>(cancellationToken, domainEvent.ProjectId, domainEvent.EmployeeId);
        if (member is null)
        {
            member = new TeamMemberItemView { ProjectId = domainEvent.ProjectId, EmployeeId = domainEvent.EmployeeId };
            store.Add(member);
        }

        // The member shows the role of the latest period.
        var periods = await store.LoadAsync<MembershipPeriodView>(
            p => p.ProjectId == domainEvent.ProjectId && p.EmployeeId == domainEvent.EmployeeId, cancellationToken);
        if (periods.MaxBy(p => p.From)?.TeamMemberId == domainEvent.TeamMemberId)
        {
            member.RoleId = domainEvent.RoleId;
            member.RoleName = role?.Name ?? string.Empty;
        }
    }

    public async Task HandleAsync(TeamMembershipEnded domainEvent, CancellationToken cancellationToken = default)
    {
        await ChangeProjectAsync(domainEvent.ProjectId, domainEvent, _ => { }, cancellationToken);

        if (await store.FindAsync<MembershipPeriodView>(cancellationToken, domainEvent.TeamMemberId) is { } period)
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
        var project = await store.FindAsync<ProjectItemView>(cancellationToken, projectId);
        if (project is null)
            return;

        change(project);
        project.Version = Math.Max(project.Version, domainEvent.AggregateVersion);
    }

    private async Task AddRateAsync(
        Guid projectId, Guid teamMemberId, Guid employeeId, DateOnly from, decimal amount, CancellationToken cancellationToken)
    {
        if (await store.FindAsync<HourlyRateItemView>(cancellationToken, teamMemberId, from) is not null)
            return;

        store.Add(new HourlyRateItemView { TeamMemberId = teamMemberId, From = from, ProjectId = projectId, EmployeeId = employeeId, Amount = amount });
    }
}
