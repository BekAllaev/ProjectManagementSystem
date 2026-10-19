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

    private readonly List<HourlyRate> _hourlyRates = [];

    /// <summary>
    /// History of rates ordered by <see cref="HourlyRate.From"/>. Existing rates are never changed or removed.
    /// </summary>
    public IReadOnlyList<HourlyRate> HourlyRates => _hourlyRates.AsReadOnly();

    internal TeamMember(Guid id, Guid employeeId, Guid roleId, DateOnly from, DateOnly? to, decimal hourlyRate) : base(id)
    {
        EnsurePeriodIsValid(from, to);

        EmployeeId = employeeId;
        RoleId = roleId;
        From = from;
        To = to;

        _hourlyRates.Add(new HourlyRate(hourlyRate, from));
    }

    public bool IsActiveOn(DateOnly date) => From <= date && (To is null || date <= To);

    public HourlyRate GetHourlyRateOn(DateOnly date)
    {
        if (!IsActiveOn(date))
            throw new DomainException($"Employee '{EmployeeId}' is not on the project on {date}.");

        return _hourlyRates.Last(r => r.From <= date);
    }

    internal bool Overlaps(DateOnly from, DateOnly? to) =>
        From <= (to ?? DateOnly.MaxValue) && from <= (To ?? DateOnly.MaxValue);

    internal void End(DateOnly to)
    {
        if (To is not null)
            throw new DomainException("Team membership has already ended.");

        EnsurePeriodIsValid(From, to);

        if (_hourlyRates[^1].From > to)
            throw new DomainException("Team membership cannot end before the start of its last hourly rate.");

        To = to;
    }

    internal HourlyRate ChangeHourlyRate(decimal amount, DateOnly from, DateOnly today)
    {
        var rate = new HourlyRate(amount, from);

        if (from < today)
            throw new DomainException("Hourly rate cannot be changed retroactively.");

        if (from <= _hourlyRates[^1].From)
            throw new DomainException("New hourly rate must start after the start of the current one.");

        if (!IsActiveOn(from))
            throw new DomainException("New hourly rate must start within the period of the team membership.");

        _hourlyRates.Add(rate);
        return rate;
    }

    private static void EnsurePeriodIsValid(DateOnly from, DateOnly? to)
    {
        if (to is not null && from > to)
            throw new DomainException("Start of the team membership must not be later than its end.");
    }
}
