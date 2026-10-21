using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Employees;

public static class EmailAddress
{
    /// <summary>
    /// The form in which emails are compared for uniqueness: trimmed and lower-case (invariant culture).
    /// </summary>
    public static string Normalize(string? email) => Guard.NotEmpty(email, "Email").ToLowerInvariant();
}
