using ProjectManagementSystem.Infrastructure.Identity;

namespace ProjectManagementSystem.Api.Authentication;

internal static class AuthEndpoints
{
    public const string Prefix = "/auth";

    /// <summary>
    /// The endpoints of <c>MapIdentityApi</c> under <see cref="Prefix"/>, without <c>/register</c>: a public
    /// registration would create an account without an employee, which the model does not allow.
    /// </summary>
    public static IEndpointConventionBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(Prefix);
        group.AddEndpointFilter(async (context, next) =>
            context.HttpContext.Request.Path.Equals($"{Prefix}/register", StringComparison.OrdinalIgnoreCase)
                ? Results.NotFound()
                : await next(context));
        group.MapIdentityApi<ApplicationUser>();

        return group;
    }
}
