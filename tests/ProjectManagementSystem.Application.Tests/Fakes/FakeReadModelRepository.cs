using System.Linq.Expressions;
using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// Rows of one read model in memory, shared by every scope of a <see cref="TestApplication"/>, like a table.
/// </summary>
public sealed class FakeReadModelRepository<TRow> : IReadModelRepository<TRow> where TRow : class
{
    private readonly List<TRow> _rows = [];

    public IReadOnlyList<TRow> All => _rows;

    public Task<List<TRow>> ListAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default) =>
        Task.FromResult(_rows.Where(predicate.Compile()).ToList());

    public async Task<TRow?> FindAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default) =>
        (await ListAsync(predicate, cancellationToken)).SingleOrDefault();

    public void Add(TRow row) => _rows.Add(row);

    public void Remove(TRow row) => _rows.Remove(row);
}
