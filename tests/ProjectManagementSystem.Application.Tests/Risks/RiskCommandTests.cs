using FluentValidation;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Tests.Risks;

public class RiskCommandTests : CommandTestBase
{
    [Fact]
    public async Task RegisterRisk_ExistingProject_AddsOpenRiskWithoutEvents()
    {
        var project = SeedProject();

        var riskId = await App.SendAsync(
            new RegisterRiskCommand(project.Id, "Key developer leaves", RiskLevel.Low, RiskLevel.High, Member));

        var risk = Assert.Single(App.Risks.All);
        Assert.Equal(riskId, risk.Id);
        Assert.Equal(project.Id, risk.ProjectId);
        Assert.Equal(RiskStatus.Open, risk.Status);
        AssertCommitted(risk);
    }

    [Fact]
    public async Task RegisterRisk_UnknownProject_ThrowsAggregateNotFoundException()
    {
        await AssertRejectedAsync<AggregateNotFoundException>(() =>
            App.SendAsync(new RegisterRiskCommand(Guid.NewGuid(), "Key developer leaves", RiskLevel.Low, RiskLevel.High, Member)));
    }

    [Fact]
    public async Task RegisterRisk_UndefinedLevel_ThrowsValidationException()
    {
        var project = SeedProject();

        await AssertRejectedAsync<ValidationException>(() =>
            App.SendAsync(new RegisterRiskCommand(project.Id, "Key developer leaves", (RiskLevel)42, RiskLevel.High, Member)));
    }

    [Fact]
    public async Task UpdateRisk_ExistingRisk_ReplacesItsFields()
    {
        var project = SeedProject();
        var risk = App.Risks.Seed(new Risk(Guid.NewGuid(), project.Id, "Old", RiskLevel.Low, RiskLevel.Low, Member, RiskStatus.Open));
        var newOwner = Guid.NewGuid();

        await App.SendAsync(new UpdateRiskCommand(risk.Id, "New", RiskLevel.High, RiskLevel.Medium, newOwner, RiskStatus.Closed));

        Assert.Equal(("New", RiskLevel.High, RiskLevel.Medium, newOwner, RiskStatus.Closed),
            (risk.Description, risk.Probability, risk.Impact, risk.OwnerId, risk.Status));
        AssertCommitted(risk);
    }
}
