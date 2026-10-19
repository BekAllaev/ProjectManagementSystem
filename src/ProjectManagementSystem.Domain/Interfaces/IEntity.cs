namespace ProjectManagementSystem.Domain.Interfaces;

public interface IEntity<TKey>
{
    public TKey Id { get; }
}
