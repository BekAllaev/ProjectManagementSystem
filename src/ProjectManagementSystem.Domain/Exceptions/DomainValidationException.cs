namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Thrown when the input of an operation is invalid by itself, whatever the state of the aggregate:
/// an empty name, a non-positive amount, a period that ends before it starts, aggregates that do not
/// belong together.
/// </summary>
public class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message)
    {
    }
}
