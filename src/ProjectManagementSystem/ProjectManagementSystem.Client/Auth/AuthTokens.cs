namespace ProjectManagementSystem.Client.Auth;

/// <summary>
/// The tokens of a sign-in, as the browser keeps them. Both tokens are opaque to the client (the bearer tokens of
/// ASP.NET Core Identity, not JWTs), so the email is kept beside them for the header until the employee is loaded.
/// </summary>
/// <param name="ExpiresAt">When <paramref name="AccessToken"/> stops being accepted, in UTC.</param>
public sealed record AuthTokens(string Email, string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// The access token is refreshed this long before it expires, so a call never leaves with a token that runs out
    /// on the way.
    /// </summary>
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(30);

    public bool NeedsRefresh(DateTimeOffset now) => now >= ExpiresAt - RefreshMargin;
}
