using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Funding.Events;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Application.Tests.Funding;

public class FundingCommandTests : CommandTestBase
{
    [Fact]
    public async Task AddFunding_FirstTranche_CreatesFundingOfProject()
    {
        var project = SeedProject();

        await App.SendAsync(new AddFundingCommand(project.Id, 10_000m, Today, "Seed round"));

        var funding = Assert.Single(App.Fundings.All);
        Assert.Equal(project.Id, funding.ProjectId);
        Assert.Equal(10_000m, funding.Tranches.Single().Amount);
        AssertCommitted(funding, typeof(FundingAdded));
    }

    [Fact]
    public async Task AddFunding_ExistingFunding_AddsTranche()
    {
        var project = SeedProject();
        var service = new FundingService();
        var funding = service.CreateFunding(project);
        service.AddFunding(project, funding, 10_000m, Today.AddDays(-30), "Seed round");
        App.Fundings.Seed(funding);

        await App.SendAsync(new AddFundingCommand(project.Id, 5_000m, Today, null));

        Assert.Equal([10_000m, 5_000m], funding.Tranches.Select(t => t.Amount));
        AssertCommitted(funding, typeof(FundingAdded));
    }

    [Fact]
    public async Task AddFunding_ClosedProject_ThrowsDomainException()
    {
        var project = SeedProject();
        TestData.Close(project);
        project.ClearDomainEvents();

        await AssertRejectedAsync<DomainException>(() => App.SendAsync(new AddFundingCommand(project.Id, 5_000m, Today, null)));
        Assert.Empty(App.Fundings.All);
    }

    [Fact]
    public async Task AddFunding_UnknownProject_ThrowsNotFoundException()
    {
        await AssertRejectedAsync<NotFoundException>(() => App.SendAsync(new AddFundingCommand(Guid.NewGuid(), 5_000m, Today, null)));
    }
}
