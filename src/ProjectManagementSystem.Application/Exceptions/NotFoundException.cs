namespace ProjectManagementSystem.Application.Exceptions;

/// <summary>
/// Thrown when a command refers to an aggregate that does not exist. A missing inner entity
/// (a task in a work package) is a domain error instead, see <c>EntityNotFoundException</c> of the domain.
/// </summary>
public sealed class NotFoundException : Exception
{
    public string EntityName { get; }

    public Guid Id { get; }

    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with id '{id}' was not found.")
    {
        EntityName = entityName;
        Id = id;
    }
}
