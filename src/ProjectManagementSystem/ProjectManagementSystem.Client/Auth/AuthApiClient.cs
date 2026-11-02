using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ProjectManagementSystem.Client.Auth;

/// <summary>
/// The HTTP endpoints of sign-in on the API (<c>MapIdentityApi</c> under <c>/auth</c>): the only calls that are not
/// gRPC, because they run before there is a token.
/// </summary>
public sealed class AuthApiClient(HttpClient http)
{
    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            // Without useCookies the API answers with bearer tokens.
            response = await http.PostAsJsonAsync("auth/login", new LoginRequest(email, password), cancellationToken);
        }
        catch (HttpRequestException)
        {
            return LoginResult.Failed(LoginFailure.ServerUnreachable);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Identity puts the reason in the detail of the problem: Failed, LockedOut, NotAllowed or RequiresTwoFactor.
                var problem = await ReadProblemAsync(response, cancellationToken);
                return LoginResult.Failed(problem?.Detail == "LockedOut" ? LoginFailure.LockedOut : LoginFailure.InvalidCredentials);
            }

            if (!response.IsSuccessStatusCode)
                return LoginResult.Failed(LoginFailure.ServerError);

            var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);
            return token is null
                ? LoginResult.Failed(LoginFailure.ServerError)
                : LoginResult.Succeeded(token.ToTokens(email));
        }
    }

    /// <summary>
    /// New tokens for <paramref name="tokens"/>; <c>null</c> if the API no longer accepts its refresh token.
    /// </summary>
    /// <exception cref="HttpRequestException">The API could not be reached; the tokens may still be valid.</exception>
    public async Task<AuthTokens?> RefreshAsync(AuthTokens tokens, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("auth/refresh", new RefreshRequest(tokens.RefreshToken), cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            return null;

        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(cancellationToken);
        return token?.ToTokens(tokens.Email);
    }

    private static async Task<ProblemDetails?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record LoginRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("password")] string Password);

    private sealed record RefreshRequest([property: JsonPropertyName("refreshToken")] string RefreshToken);

    private sealed record AccessTokenResponse(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("expiresIn")] long ExpiresIn,
        [property: JsonPropertyName("refreshToken")] string RefreshToken)
    {
        public AuthTokens ToTokens(string email) =>
            new(email, AccessToken, RefreshToken, DateTimeOffset.UtcNow.AddSeconds(ExpiresIn));
    }

    private sealed record ProblemDetails([property: JsonPropertyName("detail")] string? Detail);
}

public enum LoginFailure
{
    InvalidCredentials,
    LockedOut,
    ServerUnreachable,
    ServerError,
}

public sealed record LoginResult(AuthTokens? Tokens, LoginFailure? Failure)
{
    public bool IsSuccess => Tokens is not null;

    public static LoginResult Succeeded(AuthTokens tokens) => new(tokens, null);

    public static LoginResult Failed(LoginFailure failure) => new(null, failure);
}
