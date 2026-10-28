using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.ReadModels.Projections;

/// <summary>
/// Rows of the read models for the projections, in the <see cref="AppDbContext"/> of the delivery of one event. Nothing is
/// saved here: the outbox saves the changed rows together with the mark that the event was processed, so a projection is
/// applied atomically. Reads see the changes of the projections that ran before in the same delivery.
/// </summary>
internal sealed class ReadModelStore(AppDbContext dbContext)
{
    /// <param name="keys">In the order of the key of the row.</param>
    public async Task<T?> FindAsync<T>(CancellationToken cancellationToken, params object[] keys) where T : class =>
        await dbContext.Set<T>().FindAsync(keys, cancellationToken);

    /// <summary>
    /// The stored rows that match, merged with the rows added or changed in this delivery; removed ones are left out.
    /// </summary>
    public async Task<List<T>> LoadAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : class
    {
        var rows = dbContext.Set<T>();
        await rows.Where(predicate).LoadAsync(cancellationToken);

        return rows.Local.Where(predicate.Compile()).ToList();
    }

    public void Add<T>(T row) where T : class => dbContext.Set<T>().Add(row);

    public void Remove<T>(T row) where T : class => dbContext.Set<T>().Remove(row);
}
