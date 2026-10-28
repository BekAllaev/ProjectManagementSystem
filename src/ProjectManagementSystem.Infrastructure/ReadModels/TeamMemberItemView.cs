namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Row of the team of a project, one per employee (<c>TeamMemberItem</c>); its periods and rates are in
/// <see cref="MembershipPeriodView"/> and <see cref="HourlyRateItemView"/>.
/// </summary>
public sealed class TeamMemberItemView
{
    public Guid ProjectId { get; set; }

    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Role of the latest membership period.
    /// </summary>
    public Guid RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
