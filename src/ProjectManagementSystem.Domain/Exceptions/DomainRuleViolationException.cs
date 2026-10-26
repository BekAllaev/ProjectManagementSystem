namespace ProjectManagementSystem.Domain.Exceptions;

/// <summary>
/// Thrown when the state of an aggregate does not allow the operation: the work package is already
/// closed, the dependency would create a cycle, the name is already taken, and so on.
/// </summary>
public class DomainRuleViolationException : DomainException
{
    public DomainRuleViolationException(string message) : base(message)
    {
    }
}
