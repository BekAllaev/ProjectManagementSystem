using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProjectManagementSystem.Infrastructure.Identity;

namespace ProjectManagementSystem.Api.Authentication;

/// <summary>
/// Adds the <see cref="EmployeeClaimTypes.EmployeeId"/> claim to the principal of an account, so the token issued by
/// <c>/auth/login</c> and <c>/auth/refresh</c> carries the employee. The standard claims (the name identifier with
/// the id of the account, the security stamp) stay as they are: Identity relies on them.
/// </summary>
internal sealed class EmployeeClaimsPrincipalFactory(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(EmployeeClaimTypes.EmployeeId, user.EmployeeId.ToString()));

        return identity;
    }
}
