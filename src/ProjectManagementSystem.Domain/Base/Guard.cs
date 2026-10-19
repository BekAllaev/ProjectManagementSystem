using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Base;

internal static class Guard
{
    public static string NotEmpty(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} must not be empty.");

        return value.Trim();
    }

    public static decimal Positive(decimal value, string name)
    {
        if (value <= 0)
            throw new DomainException($"{name} must be greater than zero.");

        return value;
    }
}
