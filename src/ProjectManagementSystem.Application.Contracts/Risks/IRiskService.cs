using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;
using ProjectManagementSystem.Application.Risks.Queries.GetRisks;

namespace ProjectManagementSystem.Application.Risks;

/// <summary>
/// gRPC service of the Risk aggregate: one method per command and per query. The host implements every
/// method by sending the command or query through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.RiskService")]
public interface IRiskService
{
    Task<RiskList> GetRisks(GetRisksQuery query, CallContext context = default);

    Task<CreatedId> RegisterRisk(RegisterRiskCommand command, CallContext context = default);

    Task UpdateRisk(UpdateRiskCommand command, CallContext context = default);
}
