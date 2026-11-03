namespace ProjectManagementSystem.Client.State;

/// <summary>
/// Tells the views that notifications were read, so the badge of the app bar counts again without a reload.
/// </summary>
public sealed class NotificationEvents
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
