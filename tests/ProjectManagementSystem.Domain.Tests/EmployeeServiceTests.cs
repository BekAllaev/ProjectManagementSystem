using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class EmployeeServiceTests
{
    private readonly InMemoryRepository<Employee> _employees = new();
    private readonly EmployeeService _service;

    public EmployeeServiceTests() => _service = new EmployeeService(_employees);

    [Fact]
    public async Task Email_is_kept_as_entered_and_compared_normalized()
    {
        var employee = await _service.RegisterAsync("Ada", "Lovelace", " Ada@Example.com ");
        _employees.Add(employee);

        Assert.Equal("Ada@Example.com", employee.Email);
        Assert.Equal("ada@example.com", employee.NormalizedEmail);
        await Assert.ThrowsAsync<DomainException>(() => _service.RegisterAsync("Other", "Person", "ADA@example.COM"));
    }

    [Fact]
    public async Task Email_cannot_be_changed_to_one_of_another_employee()
    {
        var ada = await _service.RegisterAsync("Ada", "Lovelace", "ada@example.com");
        var alan = await _service.RegisterAsync("Alan", "Turing", "alan@example.com");
        _employees.Add(ada);
        _employees.Add(alan);

        await Assert.ThrowsAsync<DomainException>(() => _service.ChangeEmailAsync(alan, "Ada@Example.com"));
        Assert.Equal("alan@example.com", alan.Email);

        await _service.ChangeEmailAsync(ada, "ADA@example.com");
        Assert.Equal("ADA@example.com", ada.Email);
    }
}
