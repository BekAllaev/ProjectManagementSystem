using ProjectManagementSystem.Infrastructure.Identity;

namespace ProjectManagementSystem.Api.Authentication;

internal static class AuthEndpoints
{
    public const string Prefix = "/auth";

    /// <summary>
    /// Endpoints of <c>MapIdentityApi</c> that stay open. The others answer 404: a public registration would create an
    /// account without an employee, which the model does not allow; confirmation and reset of passwords need email,
    /// which is not set up; changing the email or password of an account waits for the flow of creating accounts.
    /// </summary>
    private static readonly HashSet<(string Method, string Path)> Allowed = new(
        [
            (HttpMethods.Post, $"{Prefix}/login"),
            (HttpMethods.Post, $"{Prefix}/refresh"),
            (HttpMethods.Get, $"{Prefix}/manage/info"),
        ],
        new EndpointComparer());

    public static IEndpointConventionBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup(Prefix).MapIdentityApi<ApplicationUser>();

    /// <summary>
    /// Answers 404 for every endpoint under <see cref="Prefix"/> that is not allowed. A middleware rather than an
    /// endpoint filter: a filter runs after the body is bound, so a closed endpoint would still answer 400 to a
    /// malformed body and give itself away.
    /// </summary>
    public static IApplicationBuilder UseAuthEndpointsAllowList(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments(Prefix, StringComparison.OrdinalIgnoreCase) &&
                !Allowed.Contains((context.Request.Method, context.Request.Path.Value ?? string.Empty)))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next(context);
        });

    private sealed class EndpointComparer : IEqualityComparer<(string Method, string Path)>
    {
        public bool Equals((string Method, string Path) x, (string Method, string Path) y) =>
            HttpMethods.Equals(x.Method, y.Method) && string.Equals(x.Path.TrimEnd('/'), y.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Method, string Path) endpoint) =>
            HashCode.Combine(endpoint.Method.ToUpperInvariant(), endpoint.Path.TrimEnd('/').ToUpperInvariant());
    }
}
