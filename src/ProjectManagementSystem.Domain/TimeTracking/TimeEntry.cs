using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.TimeTracking;

/// <summary>
/// Time spent by an employee on a task on one day. <see cref="HourlyRate"/> is a snapshot of the rate
/// of the employee valid on <see cref="WorkDate"/>, like the price in an order line: it never changes,
/// even if the rate of the employee changes later. Cost of the entry is Hours × HourlyRate.
/// </summary>
public sealed class TimeEntry : Entity<Guid>
{
    public Guid EmployeeId { get; }

    public DateOnly WorkDate { get; }

    public decimal Hours { get; }

    public decimal HourlyRate { get; }

    internal TimeEntry(Guid id, Guid employeeId, DateOnly workDate, decimal hours, decimal hourlyRate) : base(id)
    {
        EmployeeId = employeeId;
        WorkDate = workDate;
        Hours = Guard.Positive(hours, nameof(Hours));
        HourlyRate = Guard.Positive(hourlyRate, nameof(HourlyRate));
    }
}
