using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Funding;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command of <see cref="IFundingService"/> through MediatR, so the pipeline behaviors apply. No logic here.
/// </summary>
[Authorize]
public sealed class FundingGrpcService(ISender mediator) : IFundingService
{
    public Task AddFunding(AddFundingCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
