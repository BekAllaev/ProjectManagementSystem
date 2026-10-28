namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// A membership period of a team member (<c>MembershipPeriod</c>).
/// </summary>
public sealed class MembershipPeriodView
{
    public Guid TeamMemberId { get; set; }

    public Guid ProjectId { get; set; }

    public Guid EmployeeId { get; set; }

    public DateOnly From { get; set; }

    public DateOnly? To { get; set; }
}
