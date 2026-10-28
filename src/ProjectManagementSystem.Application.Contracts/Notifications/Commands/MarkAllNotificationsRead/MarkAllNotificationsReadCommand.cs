using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Notifications.Commands.MarkAllNotificationsRead;

/// <summary>
/// Marks every unread notification of the caller as read.
/// </summary>
[DataContract]
public sealed record MarkAllNotificationsReadCommand : ICommand;
