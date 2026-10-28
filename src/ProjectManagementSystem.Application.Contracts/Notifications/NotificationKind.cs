namespace ProjectManagementSystem.Application.Notifications;

/// <summary>
/// What a notification is about; the client picks its icon and wording by it. Values are stored as numbers, so a new
/// kind gets the next free value.
/// </summary>
public enum NotificationKind
{
    /// <summary>
    /// Notifications stored before the kind was recorded.
    /// </summary>
    Other = 0,

    /// <summary>
    /// The responsible person of open work has no ongoing membership in the team of the project.
    /// </summary>
    ResponsibleOutsideTeam = 1,

    /// <summary>
    /// A predecessor of an unfinished task was cancelled and the dependency was removed.
    /// </summary>
    PredecessorCancelled = 2
}
