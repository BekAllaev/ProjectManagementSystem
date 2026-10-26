using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Domain.Projects;

/// <summary>
/// ISO 4217 currency code, for example USD or EUR.
/// </summary>
public sealed record Currency
{
    public string Code { get; }

    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 3 || !code.Trim().All(char.IsLetter))
            throw new DomainValidationException($"'{code}' is not a valid currency code.");

        Code = code.Trim().ToUpperInvariant();
    }

    public override string ToString() => Code;
}
