using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Registers employees and changes their email. The email is unique among all employees (compared
/// normalized); every employee is its own aggregate, so the rule depends on the whole set and this is the
/// only domain service that uses a repository. The check here is the precondition; two concurrent
/// registrations with the same email are stopped by a unique index on the normalized email in the
/// database. Only <see cref="Employee"/> is changed.
/// </summary>
public sealed class EmployeeRegistrationService(IEmployeeRepository employees)
{
    /// <returns>New employee; the caller adds it to the repository.</returns>
    public async Task<Employee> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string? photoPath = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsFreeAsync(email, exceptEmployeeId: null, cancellationToken);

        return new Employee(Guid.NewGuid(), firstName, lastName, email, photoPath);
    }

    public async Task ChangeEmailAsync(Employee employee, string newEmail, CancellationToken cancellationToken = default)
    {
        await EnsureEmailIsFreeAsync(newEmail, employee.Id, cancellationToken);

        employee.ChangeEmail(newEmail);
    }

    private async Task EnsureEmailIsFreeAsync(string email, Guid? exceptEmployeeId, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailAddress.Normalize(email);

        if (await employees.ExistsWithEmailAsync(normalizedEmail, exceptEmployeeId, cancellationToken))
            throw new DomainException($"Email '{normalizedEmail}' is already used by another employee.");
    }
}
