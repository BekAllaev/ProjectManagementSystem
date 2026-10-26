namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Thrown when the acting person has no right to perform the operation.
/// </summary>
public class DomainForbiddenException : DomainException
{
    public DomainForbiddenException(string message) : base(message)
    {
    }
}
