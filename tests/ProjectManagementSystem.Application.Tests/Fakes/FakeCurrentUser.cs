using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// The caller of the requests; a test sets it before sending a command.
/// </summary>
public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid EmployeeId { get; set; }
}
