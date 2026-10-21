using ProjectManagementSystem.Domain.Employees;

namespace ProjectManagementSystem.Domain.Tests.Fakes;

public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<Guid, Employee> _items = [];

    public Task<Employee?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task<bool> ExistsWithEmailAsync(string normalizedEmail, Guid? exceptEmployeeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Values.Any(e => e.NormalizedEmail == normalizedEmail && e.Id != exceptEmployeeId));

    public void Add(Employee aggregate) => _items.Add(aggregate.Id, aggregate);

    public void Update(Employee aggregate)
    {
        if (!_items.ContainsKey(aggregate.Id))
            throw new InvalidOperationException($"{nameof(Employee)} '{aggregate.Id}' was never added.");
    }

    public void Delete(Employee aggregate) => _items.Remove(aggregate.Id);
}
