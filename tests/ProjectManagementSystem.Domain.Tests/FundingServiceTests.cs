using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Funding.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class FundingServiceTests
{
    private readonly FundingService _service = new();

    [Fact]
    public void Funding_is_added_to_an_open_project()
    {
        var project = TestData.ProjectWithTeam();
        var funding = _service.CreateFunding(project);

        _service.AddFunding(project, funding, 1000m, TestData.Today, "Initial");

        Assert.Equal(project.Id, funding.ProjectId);
        Assert.Equal(1000m, Assert.Single(funding.Tranches).Amount);
        Assert.Single(funding.DomainEvents.OfType<FundingAdded>());
    }

    [Fact]
    public void Closed_project_gets_no_new_funding()
    {
        var project = TestData.ProjectWithTeam();
        var funding = _service.CreateFunding(project);
        TestData.Close(project);

        Assert.Throws<DomainRuleViolationException>(() => _service.AddFunding(project, funding, 1000m, TestData.Today, "Late"));
        Assert.Empty(funding.Tranches);
    }

    [Fact]
    public void Funding_must_belong_to_the_project()
    {
        var project = TestData.ProjectWithTeam();
        var foreign = _service.CreateFunding(TestData.ProjectWithTeam());

        Assert.Throws<DomainValidationException>(() => _service.AddFunding(project, foreign, 1000m, TestData.Today, "Wrong"));
    }
}
