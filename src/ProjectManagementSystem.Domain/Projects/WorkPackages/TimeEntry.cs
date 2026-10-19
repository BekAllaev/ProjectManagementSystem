using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.WorkPackages;

/// <summary>
/// Time spent by an employee on a task. Cost of the entry (Hours × HourlyRate valid on Date)
/// is computed on the read side and is not stored.
/// </summary>
public sealed record TimeEntry
{
    public Guid EmployeeId { get; }

    public DateOnly Date { get; }

    public decimal Hours { get; }

    public TimeEntry(Guid employeeId, DateOnly date, decimal hours)
    {
        EmployeeId = employeeId;
        Date = date;
        Hours = Guard.Positive(hours, nameof(Hours));
    }
}
