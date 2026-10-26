using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectManagementSystem.Api.Authentication;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Api.Tests;

public class ClaimsCurrentUserTests
{
    [Fact]
    public void Employee_id_comes_from_the_claim_of_the_caller()
    {
        var employeeId = Guid.NewGuid();

        Assert.Equal(employeeId, CurrentUser(new Claim(EmployeeClaimTypes.EmployeeId, employeeId.ToString())).EmployeeId);
    }

    [Fact]
    public void Caller_without_the_claim_is_forbidden()
    {
        var currentUser = CurrentUser(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        Assert.Throws<DomainForbiddenException>(() => currentUser.EmployeeId);
    }

    [Fact]
    public void Caller_with_a_malformed_claim_is_forbidden()
    {
        var currentUser = CurrentUser(new Claim(EmployeeClaimTypes.EmployeeId, "not-a-guid"));

        Assert.Throws<DomainForbiddenException>(() => currentUser.EmployeeId);
    }

    [Fact]
    public void Outside_of_a_call_there_is_no_caller()
    {
        var currentUser = new ClaimsCurrentUser(new HttpContextAccessor());

        Assert.Throws<DomainForbiddenException>(() => currentUser.EmployeeId);
    }

    private static ClaimsCurrentUser CurrentUser(params Claim[] claims)
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };

        return new ClaimsCurrentUser(new HttpContextAccessor { HttpContext = httpContext });
    }
}
