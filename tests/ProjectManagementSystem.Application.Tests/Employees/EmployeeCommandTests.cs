using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;
using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;
using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Application.Tests.Employees;

public class EmployeeCommandTests : CommandTestBase
{
    [Fact]
    public async Task RegisterEmployee_UnusedEmail_AddsEmployee()
    {
        var employeeId = (await App.SendAsync(new RegisterEmployeeCommand("Grace", "Hopper", "Grace@Example.com"))).Id;

        var employee = Assert.Single(App.Employees.All);
        Assert.Equal(employeeId, employee.Id);
        Assert.Equal("grace@example.com", employee.NormalizedEmail);
        AssertCommitted(employee);
    }

    [Fact]
    public async Task RegisterEmployee_EmailUsedInAnotherCase_ThrowsDomainRuleViolationException()
    {
        await SeedEmployeeAsync("grace@example.com");

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new RegisterEmployeeCommand("Grace", "Hopper", " GRACE@example.com ")));
        Assert.Single(App.Employees.All);
    }

    [Fact]
    public async Task RenameEmployee_ExistingEmployee_RenamesIt()
    {
        var employee = await SeedEmployeeAsync("ada@example.com");

        await App.SendAsync(new RenameEmployeeCommand(employee.Id, "Augusta Ada", "King"));

        Assert.Equal(("Augusta Ada", "King"), (employee.FirstName, employee.LastName));
        AssertCommitted(employee);
    }

    [Fact]
    public async Task RenameEmployee_UnknownEmployee_ThrowsAggregateNotFoundException()
    {
        await AssertRejectedAsync<AggregateNotFoundException>(() => App.SendAsync(new RenameEmployeeCommand(Guid.NewGuid(), "Ada", "King")));
    }

    [Fact]
    public async Task ChangeEmployeeEmail_UnusedEmail_ChangesIt()
    {
        var employee = await SeedEmployeeAsync("ada@example.com");

        await App.SendAsync(new ChangeEmployeeEmailCommand(employee.Id, "ada.king@example.com"));

        Assert.Equal("ada.king@example.com", employee.Email);
        AssertCommitted(employee);
    }

    [Fact]
    public async Task ChangeEmployeeEmail_EmailOfAnotherEmployee_ThrowsDomainRuleViolationException()
    {
        var employee = await SeedEmployeeAsync("ada@example.com");
        await SeedEmployeeAsync("grace@example.com");

        await AssertRejectedAsync<DomainRuleViolationException>(() =>
            App.SendAsync(new ChangeEmployeeEmailCommand(employee.Id, "grace@example.com")));
        Assert.Equal("ada@example.com", employee.Email);
    }

    [Fact]
    public async Task ChangeEmployeePhoto_ExistingEmployee_ChangesIt()
    {
        var employee = await SeedEmployeeAsync("ada@example.com");

        await App.SendAsync(new ChangeEmployeePhotoCommand(employee.Id, "photos/ada.png"));

        Assert.Equal("photos/ada.png", employee.PhotoPath);
        AssertCommitted(employee);
    }
}
