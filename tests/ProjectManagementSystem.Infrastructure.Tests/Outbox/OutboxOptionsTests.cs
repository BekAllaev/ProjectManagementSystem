using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProjectManagementSystem.Infrastructure.Outbox;

namespace ProjectManagementSystem.Infrastructure.Tests.Outbox;

public class OutboxOptionsTests
{
    [Fact]
    public void A_backoff_list_from_the_configuration_replaces_the_default_one()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:RetryBackoff:0"] = "00:00:01",
                ["Outbox:RetryBackoff:1"] = "00:00:02",
                ["Outbox:MaxAttempts"] = "3",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOutboxOptions();

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<OutboxOptions>>().Value;

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], options.RetryBackoff);
        Assert.Equal(3, options.MaxAttempts);
        Assert.Equal(new OutboxOptions().DrainFailureBackoff, options.DrainFailureBackoff);
    }

    [Fact]
    public void Without_configuration_the_defaults_are_used()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddOutboxOptions();

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<OutboxOptions>>().Value;

        Assert.Equal(new OutboxOptions().RetryBackoff, options.RetryBackoff);
        Assert.Equal(5, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromDays(7), options.RetentionPeriod);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 60)]
    [InlineData(4, 3600)]
    [InlineData(9, 3600)]
    public void Backoff_grows_with_the_failures_and_then_repeats_the_last_step(int failures, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), OutboxOptions.Pick(new OutboxOptions().RetryBackoff, failures));
    }
}
