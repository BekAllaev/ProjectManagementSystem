using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Team;

/// <summary>
/// Cost of one hour of work of an employee on this project (cost rate, not salary).
/// There is no end date: a rate is valid until <see cref="From"/> of the next one.
/// </summary>
public sealed record HourlyRate
{
    public decimal Amount { get; }

    public DateOnly From { get; }

    public HourlyRate(decimal amount, DateOnly from)
    {
        Amount = Guard.Positive(amount, "Hourly rate");
        From = from;
    }
}
