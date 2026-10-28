using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries;

/// <summary>
/// Reads shared by several query handlers.
/// </summary>
internal static class ProjectFigures
{
    /// <exception cref="AggregateNotFoundException">The project does not exist.</exception>
    public static async Task<Project> GetProjectAsync(this AppDbContext dbContext, Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Project(projectId).SingleOrDefaultAsync(cancellationToken)
        ?? throw new AggregateNotFoundException(nameof(Project), projectId);

    /// <summary>
    /// Progress of every given project over the tasks of all its work packages; a project without tasks is missing.
    /// </summary>
    public static async Task<Dictionary<Guid, TaskProgress>> TaskProgressAsync(
        this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken)
    {
        var statuses = await dbContext.TaskStatusesOf(projectIds).ToListAsync(cancellationToken);

        return statuses
            .GroupBy(s => s.ProjectId)
            .ToDictionary(g => g.Key, g => TaskProgress.Of(g.Select(s => s.Status)));
    }

    /// <summary>
    /// Funding and actual cost of every given project; a project without funding and time has zeros.
    /// </summary>
    public static async Task<Dictionary<Guid, FundingFigures>> FundingFiguresAsync(
        this AppDbContext dbContext, IReadOnlyCollection<Guid> projectIds, CancellationToken cancellationToken)
    {
        var tranches = await dbContext.TranchesOf(projectIds).ToListAsync(cancellationToken);
        var entries = await dbContext.TimeEntriesOfProjects(projectIds).ToListAsync(cancellationToken);

        return projectIds.Distinct().ToDictionary(
            id => id,
            id => new FundingFigures(
                tranches.Where(t => t.ProjectId == id).Sum(t => t.Amount),
                entries.Where(e => e.ProjectId == id).Sum(e => e.Hours * e.HourlyRate)));
    }

    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
