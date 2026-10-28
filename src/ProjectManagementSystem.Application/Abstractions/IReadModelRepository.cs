using System.Linq.Expressions;

namespace ProjectManagementSystem.Application.Abstractions;

/// <summary>
/// Rows of one table of the read models (<c>Application/ReadModels</c>): the event handlers keep them up to date, the
/// query handlers read them. Unlike <see cref="Domain.Interfaces.IRepository{TAggregate}"/> it lists rows by a
/// predicate, because a read model is a set of rows, not an aggregate with rules. Nothing is saved here: in an event
/// handler the outbox saves the changed rows together with the mark that the event was processed.
/// </summary>
/// <remarks>
/// The aggregates without events (employees, risks) are read through it too: their tables are their read models.
/// </remarks>
public interface IReadModelRepository<TRow> where TRow : class
{
    /// <summary>
    /// The rows that match, including the ones added or changed in the current scope and not saved yet; removed ones
    /// are left out. The predicate is translated by the storage, so it uses only the columns of the row.
    /// </summary>
    Task<List<TRow>> ListAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// The one row that matches, as <see cref="ListAsync"/> sees it, or empty.
    /// </summary>
    /// <exception cref="InvalidOperationException">More than one row matches.</exception>
    Task<TRow?> FindAsync(Expression<Func<TRow, bool>> predicate, CancellationToken cancellationToken = default);

    void Add(TRow row);

    void Remove(TRow row);
}
