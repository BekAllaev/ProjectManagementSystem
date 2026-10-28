using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Rows of a read model in the <see cref="AppDbContext"/> of the current scope. Read rows are tracked, so an event handler
/// changes them in place and the next read of the same scope sees its changes before they are saved.
/// </summary>
internal sealed class EfReadModelRepository<TRow>(AppDbContext dbContext) : IReadModelRepository<TRow> where TRow : class
{
    public async Task<List<TRow>> ListAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var rows = dbContext.Set<TRow>();
        await rows.Where(predicate).LoadAsync(cancellationToken);

        return rows.Local.Where(predicate.Compile()).ToList();
    }

    public async Task<TRow?> FindAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default) =>
        (await ListAsync(predicate, cancellationToken)).SingleOrDefault();

    public void Add(TRow row) => dbContext.Set<TRow>().Add(row);

    public void Remove(TRow row) => dbContext.Set<TRow>().Remove(row);
}
