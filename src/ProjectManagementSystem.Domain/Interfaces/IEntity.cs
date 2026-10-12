namespace ProjectManagementSystem.Domain.Interfaces;

internal interface IEntity<TKey> 
{
    public TKey Id { get; }
}
