using System.Globalization;
using MudBlazor;
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

    public static StatusChip Chip(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Open => new("Open", Color.Default),
        ProjectTaskStatus.InProgress => new("In progress", Color.Primary),
        ProjectTaskStatus.Completed => new("Completed", Color.Success),
        ProjectTaskStatus.Cancelled => new("Cancelled", Color.Default, Variant.Outlined),
        _ => new(status.ToString(), Color.Default),
    };

    public static StatusChip Chip(ProjectState state) => state switch
    {
        ProjectState.Created => new("Created", Color.Default),
        ProjectState.InProgress => new("In progress", Color.Primary),
        ProjectState.Completed => new("Completed", Color.Default, Variant.Outlined),
        _ => new(state.ToString(), Color.Default),
    };
}

/// <summary>
/// How a status reads as a <see cref="MudChip{T}"/>: tinted text chips, outlined for finished or dropped work.
/// </summary>
public sealed record StatusChip(string Label, Color Color, Variant Variant = Variant.Text);
