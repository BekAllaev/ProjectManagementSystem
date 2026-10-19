using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Employees;

/// <summary>
/// Employee is not owned by a single project, so it stays a separate aggregate.
/// Projects reference employees only by id.
/// </summary>
public sealed class Employee : AggregateRoot<Guid>
{
    public string FirstName { get; private set; }

    public string LastName { get; private set; }

    public string Email { get; private set; }

    public string? PhotoPath { get; private set; }

    public Employee(Guid id, string firstName, string lastName, string email, string? photoPath = null) : base(id)
    {
        FirstName = Guard.NotEmpty(firstName, nameof(FirstName));
        LastName = Guard.NotEmpty(lastName, nameof(LastName));
        Email = Guard.NotEmpty(email, nameof(Email));
        PhotoPath = photoPath;
    }

    public void Rename(string firstName, string lastName)
    {
        FirstName = Guard.NotEmpty(firstName, nameof(FirstName));
        LastName = Guard.NotEmpty(lastName, nameof(LastName));
    }

    public void ChangeEmail(string email) => Email = Guard.NotEmpty(email, nameof(Email));

    public void ChangePhoto(string? photoPath) => PhotoPath = photoPath;
}
