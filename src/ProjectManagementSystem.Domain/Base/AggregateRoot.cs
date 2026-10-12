using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Base;

public class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey>
{
    public AggregateRoot(TKey id) : base(id)
    {
    }
}
