using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Risks;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;
using ProjectManagementSystem.Application.Risks.Queries.GetRisks;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command and query of <see cref="IRiskService"/> through MediatR, so the pipeline behaviors apply.
/// No logic here.
/// </summary>
[Authorize]
public sealed class RiskGrpcService(ISender mediator) : IRiskService
{
    public Task<RiskList> GetRisks(GetRisksQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task<CreatedId> RegisterRisk(RegisterRiskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task UpdateRisk(UpdateRiskCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
