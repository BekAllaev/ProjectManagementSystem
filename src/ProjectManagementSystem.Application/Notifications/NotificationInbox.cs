using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Notifications;

/// <summary>
/// The notifications an employee sees: those addressed to them, and those for the management of the projects whose team
/// they are a member of today. The domain has no role for the management yet, so every member sees them, and one member
/// reading such a notification reads it for all.
/// </summary>
internal static class NotificationInbox
{
    public static async Task<List<NotificationItemView>> ListAsync(
        IReadModelRepository<NotificationItemView> notifications,
        IReadModelRepository<MembershipPeriodView> periods,
        Guid employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var projectIds = (await periods.ListOnAsync(today, employeeId, cancellationToken)).Select(p => p.ProjectId).Distinct().ToList();

        return await notifications.ListAsync(
            n => n.RecipientEmployeeId == employeeId || (n.RecipientEmployeeId == null && projectIds.Contains(n.ProjectId)),
            cancellationToken);
    }
}
