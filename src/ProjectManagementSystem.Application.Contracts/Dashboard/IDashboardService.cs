using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Dashboard.Queries.GetDashboard;

namespace ProjectManagementSystem.Application.Dashboard;

/// <summary>
/// gRPC service of the start page of the caller, which reads several aggregates. The host implements every method by
/// sending the query through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.DashboardService")]
public interface IDashboardService
{
    Task<MyDashboard> GetDashboard(GetDashboardQuery query, CallContext context = default);
}
