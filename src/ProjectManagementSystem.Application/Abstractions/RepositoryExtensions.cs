using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Application.Abstractions;

public static class RepositoryExtensions
{
    public static async Task<TAggregate> GetAsync<TAggregate>(
        this IRepository<TAggregate> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<Guid> =>
        await repository.FindAsync(id, cancellationToken)
        ?? throw new EntityNotFoundException(typeof(TAggregate).Name, id);

    public static async Task<IReadOnlyList<TAggregate>> GetManyAsync<TAggregate>(
        this IRepository<TAggregate> repository,
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
        where TAggregate : AggregateRoot<Guid>
    {
        var aggregates = new List<TAggregate>();
        foreach (var id in ids)
            aggregates.Add(await repository.GetAsync(id, cancellationToken));

        return aggregates;
    }
}
