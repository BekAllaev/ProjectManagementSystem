using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.Funding;

/// <summary>
/// Actual cost is the sum of hours × hourly rate over every time entry of the project; every entry keeps the rate of
/// its work date, so past costs never change.
/// </summary>
internal sealed class GetFundingQueryHandler(AppDbContext dbContext) : IQueryHandler<GetFundingQuery, FundingSummary>
{
    public async Task<FundingSummary> Handle(GetFundingQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var funding = await dbContext.Funding(project.Id).SingleOrDefaultAsync(cancellationToken);
        var entries = await dbContext.TimeEntriesOfProjects([project.Id]).ToListAsync(cancellationToken);
        var workPackages = await dbContext.WorkPackagesOf(project.Id).ToDictionaryAsync(w => w.Id, w => w.Description, cancellationToken);
        var employees = await EmployeeDirectory.LoadAsync(dbContext, entries.Select(e => e.EmployeeId), cancellationToken);

        var tranches = funding?.Tranches.ToList() ?? [];
        var figures = new FundingFigures(tranches.Sum(t => t.Amount), entries.Sum(e => e.Hours * e.HourlyRate));

        var byMember = entries
            .GroupBy(e => e.EmployeeId)
            .Select(g => new MemberCostItem(employees.Ref(g.Key), g.Sum(e => e.Hours), g.Sum(e => e.Hours * e.HourlyRate)))
            .OrderByDescending(c => c.Cost)
            .ToList();
        var byWorkPackage = entries
            .GroupBy(e => e.WorkPackageId)
            .Select(g => new WorkPackageCostItem(
                g.Key, workPackages.GetValueOrDefault(g.Key, string.Empty), g.Sum(e => e.Hours), g.Sum(e => e.Hours * e.HourlyRate)))
            .OrderByDescending(c => c.Cost)
            .ToList();

        // Newest date first; of tranches with the same date, the one added last comes first.
        var trancheItems = Enumerable.Reverse(tranches)
            .OrderByDescending(t => t.Date)
            .Select(t => new FundingTrancheItem(t.Amount, t.Date, t.Reason))
            .ToList();

        return new FundingSummary(
            project.Id, project.Currency.Code, figures.Total, figures.ActualCost, figures.Remaining, figures.UsedPercent,
            figures.IsOverspent, trancheItems, byMember, byWorkPackage, funding?.Version ?? 0);
    }
}
