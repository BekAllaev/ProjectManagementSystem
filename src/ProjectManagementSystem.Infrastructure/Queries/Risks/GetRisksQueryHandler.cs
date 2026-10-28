using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Risks.Queries.GetRisks;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Risks;

/// <summary>
/// Risks have no events, so they are read from their aggregate.
/// </summary>
internal sealed class GetRisksQueryHandler(AppDbContext dbContext) : IQueryHandler<GetRisksQuery, RiskList>
{
    public async Task<RiskList> Handle(GetRisksQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var risks = await dbContext.RisksOf(project.Id, query.Status).ToListAsync(cancellationToken);
        var employees = await EmployeeDirectory.LoadAsync(dbContext, risks.Select(r => r.OwnerId), cancellationToken);

        var items = risks
            .Select(r =>
            {
                var score = ReadModelRules.RiskScore(r.Probability, r.Impact);
                return new RiskItem(
                    r.Id, r.Description, r.Probability, r.Impact, ReadModelRules.RiskLevelOf(score), score,
                    employees.Ref(r.OwnerId), r.Status, r.Version);
            })
            .OrderBy(r => r.Status == RiskStatus.Open ? 0 : 1)
            .ThenByDescending(r => r.Score)
            .ThenBy(r => r.Description, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new RiskList(items);
    }
}
