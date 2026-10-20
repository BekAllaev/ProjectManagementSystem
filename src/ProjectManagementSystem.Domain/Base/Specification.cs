namespace ProjectManagementSystem.Domain.Base;

/// <summary>
/// Business rule as an object, so the same rule can be checked in a domain service before a change
/// and re-checked by an event handler after the change is committed.
/// </summary>
public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);
}
