using System.Globalization;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Client.Formatting;

/// <summary>
/// How values read in the views. The client is in English, so numbers use the invariant culture.
/// </summary>
public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    private static readonly Dictionary<string, string> CurrencySymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUR"] = "€",
        ["USD"] = "$",
        ["GBP"] = "£",
        ["RUB"] = "₽",
    };

    public static string Money(decimal amount, string currency)
    {
        var sign = amount < 0 ? "−" : string.Empty;
        var value = Math.Abs(amount).ToString("#,0", Culture);

        return CurrencySymbols.TryGetValue(currency, out var symbol)
            ? $"{sign}{symbol}{value}"
            : $"{sign}{value} {currency}";
    }

    public static string Hours(decimal hours) => $"{Math.Round(hours, 2).ToString("#,0.##", Culture)} h";

    public static string Date(DateTime date) => date.ToString("dddd, d MMMM yyyy", Culture);

    /// <summary>
    /// How long ago <paramref name="utc"/> was, as the notification lists show it.
    /// </summary>
    public static string Ago(DateTime utc, DateTime nowUtc)
    {
        var elapsed = nowUtc - DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "just now",
            { TotalHours: < 1 } => $"{(int)elapsed.TotalMinutes} min ago",
            { TotalDays: < 1 } => $"{(int)elapsed.TotalHours} h ago",
            { TotalDays: < 2 } => "yesterday",
            { TotalDays: < 7 } => $"{(int)elapsed.TotalDays} days ago",
            _ => utc.ToString("dd MMM yyyy", Culture),
        };
    }

    public static string Initials(string firstName, string lastName) =>
        string.Concat(firstName.AsSpan(0, Math.Min(1, firstName.Length)), lastName.AsSpan(0, Math.Min(1, lastName.Length)))
            .ToUpperInvariant();

    public static StatusTag Tag(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Open => new("Open", "pf-tag-neutral"),
        ProjectTaskStatus.InProgress => new("In progress", "pf-tag-accent"),
        ProjectTaskStatus.Completed => new("Completed", "pf-tag-ok"),
        ProjectTaskStatus.Cancelled => new("Cancelled", "pf-tag-outline"),
        _ => new(status.ToString(), "pf-tag-neutral"),
    };

    public static StatusTag Tag(ProjectState state) => state switch
    {
        ProjectState.Created => new("Created", "pf-tag-neutral"),
        ProjectState.InProgress => new("In progress", "pf-tag-accent"),
        ProjectState.Completed => new("Completed", "pf-tag-outline"),
        _ => new(state.ToString(), "pf-tag-neutral"),
    };
}

/// <param name="CssClass">One of the <c>pf-tag-*</c> classes of <c>app.css</c>.</param>
public sealed record StatusTag(string Label, string CssClass);
