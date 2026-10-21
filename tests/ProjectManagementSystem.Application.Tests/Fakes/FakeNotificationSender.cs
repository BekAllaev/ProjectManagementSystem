using ProjectManagementSystem.Application.Abstractions;

namespace ProjectManagementSystem.Application.Tests.Fakes;

/// <summary>
/// Keeps sent notifications and, as the contract of <see cref="INotificationSender"/> requires, ignores a
/// notification whose idempotency key was already sent.
/// </summary>
public sealed class FakeNotificationSender : INotificationSender
{
    private readonly List<Notification> _sent = [];

    public IReadOnlyList<Notification> Sent => _sent;

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (_sent.All(n => n.IdempotencyKey != notification.IdempotencyKey))
            _sent.Add(notification);

        return Task.CompletedTask;
    }
}
