using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects.Team;

/// <summary>
/// One assignment of an employee to the project. If an employee leaves and comes back,
/// the return is a new record, so the whole history stays in the project.
/// </summary>
public sealed class TeamMember : Entity<Guid>
{
    public Guid EmployeeId { get; }

    public Guid RoleId { get; }

    public DateOnly From { get; }

    /// <summary>
    /// Empty while the employee is still on the project.
    /// </summary>
    public DateOnly? To { get; private set; }

    internal TeamMember(Guid id, Guid employeeId, Guid roleId, DateOnly from, DateOnly? to) : base(id)
    {
        EnsurePeriodIsValid(from, to);

        EmployeeId = employeeId;
        RoleId = roleId;
        From = from;
        To = to;
    }

    public bool IsActiveOn(DateOnly date) => From <= date && (To is null || date <= To);

    internal bool Overlaps(DateOnly from, DateOnly? to) =>
        From <= (to ?? DateOnly.MaxValue) && from <= (To ?? DateOnly.MaxValue);

    internal void End(DateOnly to)
    {
        if (To is not null)
            throw new DomainException("Team membership has already ended.");

        EnsurePeriodIsValid(From, to);
        To = to;
    }

    private static void EnsurePeriodIsValid(DateOnly from, DateOnly? to)
    {
        if (to is not null && from > to)
            throw new DomainException("Start of the team membership must not be later than its end.");
    }
}
