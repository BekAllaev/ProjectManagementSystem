using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Api.Authentication;

/// <summary>
/// The caller of the current gRPC call, read from the <see cref="EmployeeClaimTypes.EmployeeId"/> claim of its
/// principal. The claim is read only when a handler asks for it, so a command that does not need the caller works
/// for any authenticated account.
/// </summary>
internal sealed class ClaimsCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid EmployeeId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(EmployeeClaimTypes.EmployeeId)?.Value, out var employeeId)
            ? employeeId
            : throw new DomainForbiddenException("The caller is not linked to an employee.");
}
