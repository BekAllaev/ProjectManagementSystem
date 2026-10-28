using System.Linq.Expressions;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.ReadModels;

public static class ReadModelRepositoryExtensions
{
    /// <exception cref="AggregateNotFoundException">The project has no read model (yet).</exception>
    public static async Task<ProjectItemView> GetAsync(
        this IReadModelRepository<ProjectItemView> projects, Guid projectId, CancellationToken cancellationToken = default) =>
        await projects.FindAsync(p => p.Id == projectId, cancellationToken)
        ?? throw new AggregateNotFoundException(nameof(Project), projectId);

    /// <summary>
    /// Membership periods in force on <paramref name="date"/>, of one employee or of everybody.
    /// </summary>
    public static Task<List<MembershipPeriodView>> ListOnAsync(
        this IReadModelRepository<MembershipPeriodView> periods, DateOnly date, Guid? employeeId = null, CancellationToken cancellationToken = default) =>
        periods.ListAsync(InForceOn(date, employeeId), cancellationToken);

    private static Expression<Func<MembershipPeriodView, bool>> InForceOn(DateOnly date, Guid? employeeId) =>
        p => p.From <= date && (p.To == null || p.To >= date) && (employeeId == null || p.EmployeeId == employeeId);
}
