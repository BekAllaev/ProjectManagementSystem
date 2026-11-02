using System.Text.Json;
using Microsoft.JSInterop;

namespace ProjectManagementSystem.Client.Auth;

/// <summary>
/// Keeps <see cref="AuthTokens"/> in the browser: in <c>localStorage</c> when the user asked to stay signed in, so
/// they survive closing the browser, otherwise in <c>sessionStorage</c>, which the browser clears with the tab.
/// </summary>
public sealed class TokenStorage(IJSRuntime js)
{
    private const string Key = "pms.auth";
    private const string Persistent = "localStorage";
    private const string Session = "sessionStorage";

    public async ValueTask<StoredTokens?> LoadAsync()
    {
        foreach (var storage in new[] { Persistent, Session })
        {
            var json = await js.InvokeAsync<string?>($"{storage}.getItem", Key);
            if (string.IsNullOrEmpty(json))
                continue;

            try
            {
                var tokens = JsonSerializer.Deserialize<AuthTokens>(json);
                if (tokens is not null)
                    return new StoredTokens(tokens, storage == Persistent);
            }
            catch (JsonException)
            {
                // Written by an older client: forget it, the user signs in again.
            }

            await js.InvokeVoidAsync($"{storage}.removeItem", Key);
        }

        return null;
    }

    public async ValueTask SaveAsync(AuthTokens tokens, bool persistent)
    {
        await js.InvokeVoidAsync($"{(persistent ? Persistent : Session)}.setItem", Key, JsonSerializer.Serialize(tokens));
        await js.InvokeVoidAsync($"{(persistent ? Session : Persistent)}.removeItem", Key);
    }

    public async ValueTask ClearAsync()
    {
        await js.InvokeVoidAsync($"{Persistent}.removeItem", Key);
        await js.InvokeVoidAsync($"{Session}.removeItem", Key);
    }
}

/// <param name="Persistent">Kept in <c>localStorage</c>, so a refresh keeps it there.</param>
public sealed record StoredTokens(AuthTokens Tokens, bool Persistent);
