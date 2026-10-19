namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Thrown when an aggregate is asked to operate on an inner entity it does not contain.
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.")
    {
    }
}
