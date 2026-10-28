using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.Funding.Queries.GetFunding;

public sealed class GetFundingQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<FundingTrancheItemView> tranches,
    IReadModelRepository<MemberCostItemView> memberCosts,
    IReadModelRepository<WorkPackageCostItemView> workPackageCosts,
    IReadModelRepository<Employee> employees) : IQueryHandler<GetFundingQuery, FundingSummary>
{
    public async Task<FundingSummary> Handle(GetFundingQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var projectTranches = await tranches.ListAsync(t => t.ProjectId == project.Id, cancellationToken);
        var byMember = await memberCosts.ListAsync(c => c.ProjectId == project.Id, cancellationToken);
        var byWorkPackage = await workPackageCosts.ListAsync(c => c.ProjectId == project.Id, cancellationToken);
        var names = await EmployeeDirectory.LoadAsync(employees, byMember.Select(c => c.EmployeeId), cancellationToken);

        return new FundingSummary(
            project.Id, project.Currency, project.TotalFunding, project.ActualCost, project.RemainingFunding, project.UsedPercent,
            project.IsOverspent,
            // Newest date first; of tranches with the same date, the one added last comes first.
            projectTranches.OrderByDescending(t => t.Date).ThenByDescending(t => t.Position)
                .Select(t => new FundingTrancheItem(t.Amount, t.Date, t.Reason)).ToList(),
            byMember.OrderByDescending(c => c.Cost)
                .Select(c => new MemberCostItem(names.Ref(c.EmployeeId), c.Hours, c.Cost)).ToList(),
            byWorkPackage.OrderByDescending(c => c.Cost)
                .Select(c => new WorkPackageCostItem(c.WorkPackageId, c.Description, c.Hours, c.Cost)).ToList(),
            project.FundingVersion);
    }
}
