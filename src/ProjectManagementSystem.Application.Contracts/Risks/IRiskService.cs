using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;

namespace ProjectManagementSystem.Application.Risks;

/// <summary>
/// gRPC service of the Risk aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.RiskService")]
public interface IRiskService
{
    Task<CreatedId> RegisterRisk(RegisterRiskCommand command, CallContext context = default);

    Task UpdateRisk(UpdateRiskCommand command, CallContext context = default);
}
