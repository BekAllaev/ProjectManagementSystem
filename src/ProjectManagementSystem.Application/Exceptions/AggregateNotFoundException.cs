using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Application.Exceptions;

/// <summary>
/// Thrown when a command refers to an aggregate that does not exist. Whether an aggregate exists is found out
/// by the handler and the repository, not by an aggregate, so this error lives in the application layer.
/// A missing inner entity (a task in a work package) is a domain error instead, see <see cref="EntityNotFoundException"/>.
/// </summary>
public sealed class AggregateNotFoundException : DomainException
{
    public string EntityName { get; }

    public Guid Id { get; }

    public AggregateNotFoundException(string entityName, Guid id)
        : base($"{entityName} with id '{id}' was not found.")
    {
        EntityName = entityName;
        Id = id;
    }
}
