namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// The employee on whose behalf the current request runs. It never comes from the command: the host takes it
/// from the authenticated caller. A handler only passes it to the domain, which decides whether the employee may
/// perform the operation.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Id of the <see cref="Domain.Employees.Employee"/> of the caller.
    /// </summary>
    Guid EmployeeId { get; }
}
