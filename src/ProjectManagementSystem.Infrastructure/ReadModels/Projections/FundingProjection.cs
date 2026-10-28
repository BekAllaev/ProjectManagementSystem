using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Funding.Events;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Keeps the tranches of the ProjectFunding aggregate. A tranche has no id of its own, so its row is keyed by the id of
/// the event, which makes a repeated delivery harmless. The refresh of the project then sums the funding figures. The
/// version of the funding is kept on the row of the project.
/// </summary>
internal sealed class FundingProjection(ReadModelStore store, ProjectReadModelRefresher refresher) : IDomainEventHandler<FundingAdded>
{
    public async Task HandleAsync(FundingAdded domainEvent, CancellationToken cancellationToken = default)
    {
        if (await store.FindAsync<FundingTrancheItemView>(cancellationToken, domainEvent.EventId) is null)
        {
            store.Add(new FundingTrancheItemView
            {
                EventId = domainEvent.EventId,
                ProjectId = domainEvent.ProjectId,
                Amount = domainEvent.Amount,
                Date = domainEvent.Date,
                Reason = domainEvent.Reason ?? string.Empty,
                AddedAt = domainEvent.OccurredAt,
            });
        }

        if (await store.FindAsync<ProjectItemView>(cancellationToken, domainEvent.ProjectId) is { } project)
            project.FundingVersion = Math.Max(project.FundingVersion, domainEvent.AggregateVersion);

        await refresher.RefreshAsync(domainEvent.ProjectId, cancellationToken);
    }
}
