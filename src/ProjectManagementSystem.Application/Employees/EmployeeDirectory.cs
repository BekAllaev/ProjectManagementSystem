using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Application.Employees;

/// <summary>
/// Names of the employees a read model mentions, read in one query. Employees have no events, so they are read from
/// their aggregate. An unknown id gets empty names rather than failing the whole read model.
/// </summary>
internal sealed class EmployeeDirectory
{
    private readonly Dictionary<Guid, Employee> _employees;

    private EmployeeDirectory(Dictionary<Guid, Employee> employees) => _employees = employees;

    public static async Task<EmployeeDirectory> LoadAsync(
        IReadModelRepository<Employee> employees, IEnumerable<Guid> employeeIds, CancellationToken cancellationToken)
    {
        var ids = employeeIds.Distinct().ToList();
        var found = ids.Count == 0 ? [] : await employees.ListAsync(e => ids.Contains(e.Id), cancellationToken);

        return new EmployeeDirectory(found.ToDictionary(e => e.Id));
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
