using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

public class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; init; }

    public Entity(TKey id)
    {
        Id = id;
    }

    /// <summary>
    /// For EF Core materialization only: the id is set from the stored value.
    /// </summary>
    protected Entity()
    {
        Id = default!;
    }
}
