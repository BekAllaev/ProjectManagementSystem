namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the time log of a task (<c>TimeEntryItem</c>).
/// </summary>
public sealed class TimeEntryItemView
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Guid WorkPackageId { get; set; }

    public Guid TaskId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateOnly WorkDate { get; set; }

    public decimal Hours { get; set; }

    /// <summary>
    /// Rate valid on the work date, kept as it was logged.
    /// </summary>
    public decimal HourlyRate { get; set; }

    /// <summary>
    /// Hours × hourly rate.
    /// </summary>
    public decimal Cost { get; set; }
}
