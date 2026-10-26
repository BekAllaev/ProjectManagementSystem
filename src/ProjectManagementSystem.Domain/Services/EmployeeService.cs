using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Operations on <see cref="Employee"/> whose rules need other employees. The email is unique among all
/// employees (compared normalized); every employee is its own aggregate, so the rule depends on the whole
/// set and this is the only domain service that uses a repository. The check here is the precondition;
/// two concurrent registrations with the same email are stopped by a unique index on the normalized email
/// in the database. Only <see cref="Employee"/> is changed.
/// </summary>
public sealed class EmployeeService(IRepository<Employee> employees)
{
    /// <returns>New employee; the caller adds it to the repository.</returns>
    public async Task<Employee> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string? photoPath = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailAddress.Normalize(email);

        if (await employees.ExistsAsync(e => e.NormalizedEmail == normalizedEmail, cancellationToken))
            throw EmailIsTaken(normalizedEmail);

        return new Employee(Guid.NewGuid(), firstName, lastName, email, photoPath);
    }

    public async Task ChangeEmailAsync(Employee employee, string newEmail, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailAddress.Normalize(newEmail);
        var employeeId = employee.Id;

        if (await employees.ExistsAsync(e => e.NormalizedEmail == normalizedEmail && e.Id != employeeId, cancellationToken))
            throw EmailIsTaken(normalizedEmail);

        employee.ChangeEmail(newEmail);
    }

    /// <summary>
    /// Error of a taken email. Also thrown by the persistence when the unique index stops a concurrent registration
    /// or change of the email, so the caller sees the same error either way.
    /// </summary>
    public static DomainRuleViolationException EmailIsTaken(string normalizedEmail) =>
        new($"Email '{normalizedEmail}' is already used by another employee.");
}
