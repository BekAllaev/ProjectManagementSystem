using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Queries.Notifications;

/// <summary>
/// The notifications an employee sees: those addressed to them, and those for the management of the projects whose team
/// they are a member of today. The domain has no role for the management yet, so every member sees them, and one member
/// reading such a notification reads it for all.
/// </summary>
internal static class NotificationInbox
{
    public static async Task<IQueryable<NotificationRecord>> NotificationsOfAsync(
        this AppDbContext dbContext, Guid employeeId, DateOnly today, CancellationToken cancellationToken)
    {
        var projectIds = await dbContext.MembershipsOn(today, employeeId)
            .Select(m => m.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return dbContext.NotificationsOf(employeeId, projectIds);
    }
}
