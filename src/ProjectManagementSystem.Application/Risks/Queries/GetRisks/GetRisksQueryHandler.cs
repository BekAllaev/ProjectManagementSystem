using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Queries.GetRisks;

/// <summary>
/// Risks have no events, so they are read from their aggregate; the score and level are computed on the read.
/// </summary>
public sealed class GetRisksQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<Risk> risks,
    IReadModelRepository<Employee> employees) : IQueryHandler<GetRisksQuery, RiskList>
{
    public async Task<RiskList> Handle(GetRisksQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var status = query.Status;
        var rows = await risks.ListAsync(r => r.ProjectId == project.Id && (status == null || r.Status == status), cancellationToken);
        var names = await EmployeeDirectory.LoadAsync(employees, rows.Select(r => r.OwnerId), cancellationToken);

        return new RiskList(rows
            .Select(r =>
            {
                var score = ReadModelRules.RiskScore(r.Probability, r.Impact);
                return new RiskItem(
                    r.Id, r.Description, r.Probability, r.Impact, ReadModelRules.RiskLevelOf(score), score,
                    names.Ref(r.OwnerId), r.Status, r.Version);
            })
            .OrderBy(r => r.Status == RiskStatus.Open ? 0 : 1)
            .ThenByDescending(r => r.Score)
            .ThenBy(r => r.Description, StringComparer.CurrentCultureIgnoreCase)
            .ToList());
    }
}
