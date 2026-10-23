using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Employees;

/// <summary>
/// Employee is not owned by a single project, so it stays a separate aggregate.
/// Projects reference employees only by id. The email is unique among all employees, so the employee is
/// created and its email is changed through <see cref="Services.EmployeeService"/>.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>
{
    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    /// <summary>
    /// Email as entered.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Email in the form in which it is unique among all employees, see <see cref="EmailAddress.Normalize"/>.
    /// </summary>
    public string NormalizedEmail { get; private set; }

    public string? PhotoPath { get; private set; }

    /// <summary>
    /// For EF Core materialization only; does not raise domain events.
    /// </summary>
    private Employee()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        NormalizedEmail = null!;
    }

    internal Employee(Guid id, string firstName, string lastName, string email, string? photoPath = null) : base(id)
    {
        FirstName = Guard.NotEmpty(firstName, nameof(FirstName));
        LastName = Guard.NotEmpty(lastName, nameof(LastName));
        Email = Guard.NotEmpty(email, nameof(Email));
        NormalizedEmail = EmailAddress.Normalize(email);
        PhotoPath = photoPath;
    }

    public void Rename(string firstName, string lastName)
    {
        FirstName = Guard.NotEmpty(firstName, nameof(FirstName));
        LastName = Guard.NotEmpty(lastName, nameof(LastName));
    }

    internal void ChangeEmail(string email)
    {
        Email = Guard.NotEmpty(email, nameof(Email));
        NormalizedEmail = EmailAddress.Normalize(email);
    }

    public void ChangePhoto(string? photoPath) => PhotoPath = photoPath;
}
