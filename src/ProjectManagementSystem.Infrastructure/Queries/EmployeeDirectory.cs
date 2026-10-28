using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries;

/// <summary>
/// Names of the employees a read model mentions, read in one query. An unknown id gets empty names rather than failing
/// the whole read model.
/// </summary>
internal sealed class EmployeeDirectory
{
    private readonly Dictionary<Guid, Employee> _employees;

    private EmployeeDirectory(Dictionary<Guid, Employee> employees) => _employees = employees;

    public static async Task<EmployeeDirectory> LoadAsync(
        AppDbContext dbContext, IEnumerable<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        var employees = ids.Count == 0
            ? []
            : await dbContext.Employees(ids).ToDictionaryAsync(e => e.Id, cancellationToken);

        return new EmployeeDirectory(employees);
    }

    public EmployeeRef Ref(Guid employeeId) =>
        _employees.TryGetValue(employeeId, out var employee)
            ? new EmployeeRef(employee.Id, employee.FirstName, employee.LastName)
            : new EmployeeRef(employeeId, string.Empty, string.Empty);

    public string Email(Guid employeeId) => _employees.GetValueOrDefault(employeeId)?.Email ?? string.Empty;

    /// <summary>
    /// For ordering by name.
    /// </summary>
    public string SortName(Guid employeeId) =>
        _employees.TryGetValue(employeeId, out var employee) ? $"{employee.LastName} {employee.FirstName}" : string.Empty;
}
