using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ProjectManagementSystem.Client.Auth;

/// <summary>
/// Who is signed in, for the router and the views, and the bearer token for the gRPC calls. The user counts as
/// signed in while tokens are stored; the API decides whether they are still good. When it no longer accepts them,
/// the user is signed out and lands on the login page.
/// </summary>
public sealed class AuthService(TokenStorage storage, AuthApiClient api) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private Task? _loading;
    private AuthTokens? _tokens;
    private bool _persistent;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureLoadedAsync();

        return StateOf(_tokens);
    }

    public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default)
    {
        var result = await api.LoginAsync(email, password, cancellationToken);
        if (result.Tokens is null)
            return result;

        await EnsureLoadedAsync();
        await SetTokensAsync(result.Tokens, rememberMe, userChanged: true);

        return result;
    }

    public Task LogoutAsync() => SetTokensAsync(null, persistent: false, userChanged: true);

    /// <summary>
    /// The access token for the next call, refreshed first if it is about to expire; <c>null</c> when nobody is
    /// signed in or the API no longer accepts the refresh token (then the user is signed out).
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync();

        var tokens = _tokens;
        if (tokens is null || !tokens.NeedsRefresh(DateTimeOffset.UtcNow))
            return tokens?.AccessToken;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another call may have refreshed them while this one waited.
            if (_tokens is null || !_tokens.NeedsRefresh(DateTimeOffset.UtcNow))
                return _tokens?.AccessToken;

            AuthTokens? refreshed;
            try
            {
                refreshed = await api.RefreshAsync(_tokens, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // The API is not reachable: the call fails on its own and says so; the user stays signed in.
                return _tokens.AccessToken;
            }

            // A refresh keeps the user; only a rejected one signs them out.
            await SetTokensAsync(refreshed, _persistent, userChanged: refreshed is null);
            return refreshed?.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>
    /// The API answered a call with Unauthenticated although a token was sent: the sign-in has ended on the server.
    /// </summary>
    public Task OnSessionRejectedAsync() => _tokens is null ? Task.CompletedTask : LogoutAsync();

    private Task EnsureLoadedAsync() => _loading ??= LoadAsync();

    private async Task LoadAsync()
    {
        var stored = await storage.LoadAsync();
        _tokens = stored?.Tokens;
        _persistent = stored?.Persistent ?? false;
    }

    private async Task SetTokensAsync(AuthTokens? tokens, bool persistent, bool userChanged)
    {
        _tokens = tokens;
        _persistent = persistent;

        if (tokens is null)
            await storage.ClearAsync();
        else
            await storage.SaveAsync(tokens, persistent);

        if (userChanged)
            NotifyAuthenticationStateChanged(Task.FromResult(StateOf(tokens)));
    }

    private static AuthenticationState StateOf(AuthTokens? tokens) =>
        tokens is null
            ? Anonymous
            : new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, tokens.Email), new Claim(ClaimTypes.Email, tokens.Email)],
                authenticationType: "Bearer")));
}
