using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Funding.Events;

namespace ProjectManagementSystem.Application.Funding.EventHandlers;

/// <summary>
/// Reacts to the events of the ProjectFunding aggregate by keeping its tranches in the read models. A tranche has no id
/// of its own, so its row is keyed by the id of the event, which makes a repeated delivery harmless. The refresh of the
/// project then sums the funding figures. The version of the funding is kept on the row of the project.
/// </summary>
public sealed class ProjectFundingEventHandler(
    IReadModelRepository<FundingTrancheItemView> trancheItems,
    IReadModelRepository<ProjectItemView> projectItems,
    ProjectReadModelRefresher refresher) : IDomainEventHandler<FundingAdded>
{
    public async Task HandleAsync(FundingAdded domainEvent, CancellationToken cancellationToken = default)
    {
        if (await trancheItems.FindAsync(t => t.EventId == domainEvent.EventId, cancellationToken) is null)
        {
            var existing = await trancheItems.ListAsync(t => t.ProjectId == domainEvent.ProjectId, cancellationToken);
            trancheItems.Add(new FundingTrancheItemView
            {
                EventId = domainEvent.EventId,
                ProjectId = domainEvent.ProjectId,
                Amount = domainEvent.Amount,
                Date = domainEvent.Date,
                Reason = domainEvent.Reason ?? string.Empty,
                Position = existing.Count + 1,
            });
        }

        if (await projectItems.FindAsync(p => p.Id == domainEvent.ProjectId, cancellationToken) is { } project)
            project.FundingVersion = Math.Max(project.FundingVersion, domainEvent.AggregateVersion);

        await refresher.RefreshAsync(domainEvent.ProjectId, cancellationToken);
    }
}
