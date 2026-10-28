using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Persistence.Notifications;

namespace ProjectManagementSystem.Infrastructure.Tests.Notifications;

/// <summary>
/// The sender against the EF in-memory provider: one context stands for the scope of one delivery of an event.
/// </summary>
public sealed class NotificationSenderTests : IDisposable
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 3, 2, 9, 0, 0, TimeSpan.Zero));
    private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    private readonly Notification _notification = new(
        "predecessor-cancelled:event:recipient", Guid.NewGuid(), Guid.NewGuid(), NotificationKind.PredecessorCancelled,
        "A predecessor task was cancelled", "Check the plan.", Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Adds_the_notification_without_saving_it()
    {
        await using var scope = new AppDbContext(_options);

        await new NotificationSender(scope, _clock).SendAsync(_notification);

        var added = Assert.Single(scope.Notifications.Local);
        Assert.Equal(EntityState.Added, scope.Entry(added).State);
        Assert.Equal(_notification.IdempotencyKey, added.IdempotencyKey);
        Assert.Equal(_notification.ProjectId, added.ProjectId);
        Assert.Equal(_notification.RecipientEmployeeId, added.RecipientEmployeeId);
        Assert.Equal(_notification.Kind, added.Kind);
        Assert.Equal(_notification.Title, added.Title);
        Assert.Equal(_notification.Message, added.Message);
        Assert.Equal(_notification.WorkPackageId, added.WorkPackageId);
        Assert.Equal(_notification.TaskId, added.TaskId);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, added.CreatedAt);
        Assert.Null(added.ReadAt);
        Assert.Equal(0, await CountStoredAsync());
    }

    [Fact]
    public async Task Repeated_delivery_does_not_create_a_duplicate()
    {
        await DeliverAsync();
        _clock.Advance(TimeSpan.FromMinutes(1));

        await DeliverAsync();

        Assert.Equal(1, await CountStoredAsync());
    }

    [Fact]
    public async Task The_same_notification_twice_in_one_scope_is_added_once()
    {
        await using var scope = new AppDbContext(_options);
        var sender = new NotificationSender(scope, _clock);

        await sender.SendAsync(_notification);
        await sender.SendAsync(_notification with { Message = "Another text, same key." });
        await scope.SaveChangesAsync();

        Assert.Equal(1, await CountStoredAsync());
    }

    [Fact]
    public async Task Notifications_with_different_keys_are_all_kept()
    {
        await using var scope = new AppDbContext(_options);
        var sender = new NotificationSender(scope, _clock);

        await sender.SendAsync(_notification);
        await sender.SendAsync(_notification with { IdempotencyKey = "other", RecipientEmployeeId = null });
        await scope.SaveChangesAsync();

        Assert.Equal(2, await CountStoredAsync());
    }

    public void Dispose()
    {
        using var context = new AppDbContext(_options);
        context.Database.EnsureDeleted();
    }

    /// <summary>
    /// One delivery: a new scope, the handler notifies, the processor saves.
    /// </summary>
    private async Task DeliverAsync()
    {
        await using var scope = new AppDbContext(_options);
        await new NotificationSender(scope, _clock).SendAsync(_notification);
        await scope.SaveChangesAsync();
    }

    private async Task<int> CountStoredAsync()
    {
        await using var context = new AppDbContext(_options);
        return await context.Notifications.CountAsync();
    }
}
