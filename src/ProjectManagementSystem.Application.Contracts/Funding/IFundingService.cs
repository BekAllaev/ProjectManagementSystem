using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Funding.Queries.GetFunding;

namespace ProjectManagementSystem.Application.Funding;

/// <summary>
/// gRPC service of the ProjectFunding aggregate: one method per command and per query. The host implements every
/// method by sending the command or query through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.FundingService")]
public interface IFundingService
{
    Task AddFunding(AddFundingCommand command, CallContext context = default);

    Task<FundingSummary> GetFunding(GetFundingQuery query, CallContext context = default);
}
