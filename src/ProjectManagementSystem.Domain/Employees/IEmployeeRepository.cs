using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Employees;

/// <summary>
/// Uniqueness of the email spans all employees, so it cannot be checked inside one of them; this is the
/// only search a domain service uses.
/// </summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    /// <param name="normalizedEmail">Email normalized by <see cref="EmailAddress.Normalize"/>.</param>
    /// <param name="exceptEmployeeId">Employee whose own email is not counted.</param>
    Task<bool> ExistsWithEmailAsync(string normalizedEmail, Guid? exceptEmployeeId, CancellationToken cancellationToken = default);
}
