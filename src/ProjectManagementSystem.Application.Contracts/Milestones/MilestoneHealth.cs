namespace ProjectManagementSystem.Application.Milestones;

/// <summary>
/// State of a milestone as the read side computes it from its status, target date and tasks. Only cancelled tasks are
/// not counted.
/// </summary>
public enum MilestoneHealth
{
    /// <summary>
    /// Open and the target date has not passed.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Open although the target date has passed.
    /// </summary>
    Late = 1,

    /// <summary>
    /// Completed with every counted task completed.
    /// </summary>
    Reached = 2,

    /// <summary>
    /// Completed while some counted tasks are unfinished ("closed with loose ends").
    /// </summary>
    CompletedWithLooseEnds = 3,

    /// <summary>
    /// Completed without counted tasks; an empty milestone is not considered reached.
    /// </summary>
    CompletedEmpty = 4
}
