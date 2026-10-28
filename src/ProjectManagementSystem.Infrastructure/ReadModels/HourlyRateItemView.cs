namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// An hourly rate of a membership period (<c>HourlyRateItem</c>), valid until the next one.
/// </summary>
public sealed class HourlyRateItemView
{
    public Guid TeamMemberId { get; set; }

    public DateOnly From { get; set; }

    public Guid ProjectId { get; set; }

    public Guid EmployeeId { get; set; }

    public decimal Amount { get; set; }
}
