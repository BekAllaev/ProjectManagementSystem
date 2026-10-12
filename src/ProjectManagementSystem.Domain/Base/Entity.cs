using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

public class Entity<TKey> : IEntity<TKey>
{
    public TKey Id { get; init; }

    public Entity(TKey id)
    {
        Id = id;
    }
}
