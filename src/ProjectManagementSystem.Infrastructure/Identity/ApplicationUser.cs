using Microsoft.AspNetCore.Identity;

namespace ProjectManagementSystem.Infrastructure.Identity;

/// <summary>
/// Account of a person who signs in. The account and the employee are different records with different ids: the
/// account belongs to exactly one employee, and an employee has at most one account. The domain knows nothing of
/// accounts, so <c>Employee</c> has no reference back.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Id of the <c>Employee</c> the account belongs to.
    /// </summary>
    public Guid EmployeeId { get; set; }
}
