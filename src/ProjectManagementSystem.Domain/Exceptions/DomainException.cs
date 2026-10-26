namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Base of all errors of the domain. Only its subclasses are thrown; the subclass says what kind of error
/// it is, so the API layer can translate it without knowing the rule.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
