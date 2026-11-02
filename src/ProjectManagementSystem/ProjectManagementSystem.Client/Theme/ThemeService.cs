using Microsoft.JSInterop;

namespace ProjectManagementSystem.Client.Theme;

/// <summary>
/// Dark or light, as the user chose it in the menu of the header; dark by default, as designed. The choice is kept
/// in <c>localStorage</c>, and the <c>data-theme</c> attribute of the page switches the tokens of <c>app.css</c>
/// together with the palette of MudBlazor. The script in the head of App.razor sets the attribute before the app
/// starts, so a light page does not flash dark.
/// </summary>
public sealed class ThemeService(IJSRuntime js)
{
    private const string Key = "pms.theme";

    private Task? _loading;

    public bool IsDarkMode { get; private set; } = true;

    public event Action? Changed;

    public Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    public async Task SetDarkModeAsync(bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        Changed?.Invoke();

        await js.InvokeVoidAsync("localStorage.setItem", Key, isDarkMode ? "dark" : "light");
        await ApplyAsync();
    }

    private async Task LoadAsync()
    {
        IsDarkMode = await js.InvokeAsync<string?>("localStorage.getItem", Key) != "light";
        await ApplyAsync();
        Changed?.Invoke();
    }

    private ValueTask ApplyAsync() =>
        js.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", IsDarkMode ? "dark" : "light");
}
