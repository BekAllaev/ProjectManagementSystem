using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Funding;

internal sealed class GetFundingQueryHandler(AppDbContext dbContext) : IQueryHandler<GetFundingQuery, FundingSummary>
{
    public async Task<FundingSummary> Handle(GetFundingQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var tranches = await dbContext.FundingTranchesOf(project.Id).ToListAsync(cancellationToken);
        var byMember = await dbContext.MemberCostsOf(project.Id).ToListAsync(cancellationToken);
        var byWorkPackage = await dbContext.WorkPackageCostsOf(project.Id).ToListAsync(cancellationToken);
        var employees = await EmployeeDirectory.LoadAsync(dbContext, byMember.Select(c => c.EmployeeId), cancellationToken);

        return new FundingSummary(
            project.Id, project.Currency, project.TotalFunding, project.ActualCost, project.RemainingFunding, project.UsedPercent,
            project.IsOverspent,
            // Newest date first; of tranches with the same date, the one added last comes first.
            tranches.OrderByDescending(t => t.Date).ThenByDescending(t => t.AddedAt)
                .Select(t => new FundingTrancheItem(t.Amount, t.Date, t.Reason)).ToList(),
            byMember.OrderByDescending(c => c.Cost)
                .Select(c => new MemberCostItem(employees.Ref(c.EmployeeId), c.Hours, c.Cost)).ToList(),
            byWorkPackage.OrderByDescending(c => c.Cost)
                .Select(c => new WorkPackageCostItem(c.WorkPackageId, c.Description, c.Hours, c.Cost)).ToList(),
            project.FundingVersion);
    }
}
