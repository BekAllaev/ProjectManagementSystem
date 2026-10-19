namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would break a business rule (invariant) of the domain.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
