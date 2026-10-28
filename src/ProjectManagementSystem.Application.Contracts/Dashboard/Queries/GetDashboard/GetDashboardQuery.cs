using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;

/// <summary>
/// Start page of the caller. The latest notifications come from <c>GetNotificationsQuery</c>.
/// </summary>
[DataContract]
public sealed record GetDashboardQuery : IQuery<MyDashboard>;
