using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Employees;

namespace ProjectManagementSystem.Application.Projects.Queries.GetTeam;

/// <param name="Version">Version of the project, which holds the team.</param>
[DataContract]
public sealed record Team(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] IReadOnlyList<TeamMemberItem> Members,
    [property: DataMember(Order = 3)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private Team() : this(default, new List<TeamMemberItem>(), default)
    {
    }
}

/// <param name="RoleId">Role of the latest membership period.</param>
/// <param name="IsActive">Member today.</param>
/// <param name="CurrentHourlyRate">Rate valid today, or the last rate of a former member.</param>
/// <param name="HourlyRates">All rates of all periods, newest first.</param>
[DataContract]
public sealed record TeamMemberItem(
    [property: DataMember(Order = 1)] EmployeeRef Employee,
    [property: DataMember(Order = 2)] string Email,
    [property: DataMember(Order = 3)] Guid RoleId,
    [property: DataMember(Order = 4)] string RoleName,
    [property: DataMember(Order = 5)] bool IsActive,
    [property: DataMember(Order = 6)] IReadOnlyList<MembershipPeriod> Periods,
    [property: DataMember(Order = 7)] decimal CurrentHourlyRate,
    [property: DataMember(Order = 8)] DateOnly CurrentHourlyRateFrom,
    [property: DataMember(Order = 9)] IReadOnlyList<HourlyRateItem> HourlyRates)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TeamMemberItem() : this(
        default!, default!, default, default!, default, new List<MembershipPeriod>(), default, default,
        new List<HourlyRateItem>())
    {
    }
}

/// <param name="To">Empty while the period goes on.</param>
[DataContract]
public sealed record MembershipPeriod(
    [property: DataMember(Order = 1)] Guid TeamMemberId,
    [property: DataMember(Order = 2)] DateOnly From,
    [property: DataMember(Order = 3)] DateOnly? To)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private MembershipPeriod() : this(default, default, default)
    {
    }
}

[DataContract]
public sealed record HourlyRateItem(
    [property: DataMember(Order = 1)] decimal Amount,
    [property: DataMember(Order = 2)] DateOnly From)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private HourlyRateItem() : this(default, default)
    {
    }
}
