using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Dashboard;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every query of <see cref="IDashboardService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class DashboardGrpcService(ISender mediator) : IDashboardService
{
    public Task<MyDashboard> GetDashboard(GetDashboardQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);
}
