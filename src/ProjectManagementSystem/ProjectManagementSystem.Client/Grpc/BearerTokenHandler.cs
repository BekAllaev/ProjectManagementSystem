using System.Net.Http.Headers;
using ProjectManagementSystem.Client.Auth;

namespace ProjectManagementSystem.Client.Grpc;

/// <summary>
/// Puts the bearer token of the signed-in user on every gRPC-Web request, refreshing it first when it is about to
/// expire. A request without a user goes out without a token, and the API answers Unauthenticated.
/// </summary>
internal sealed class BearerTokenHandler(AuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await auth.GetAccessTokenAsync(cancellationToken);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
