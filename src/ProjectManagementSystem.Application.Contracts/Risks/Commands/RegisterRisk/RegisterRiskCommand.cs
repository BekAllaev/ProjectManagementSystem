using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;

/// <param name="OwnerId">Employee who owns the risk.</param>
/// <returns>Id of the new risk.</returns>
[DataContract]
public sealed record RegisterRiskCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] RiskLevel Probability,
    [property: DataMember(Order = 4)] RiskLevel Impact,
    [property: DataMember(Order = 5)] Guid OwnerId) : ICommand<CreatedId>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private RegisterRiskCommand() : this(default, default!, default, default, default)
    {
    }
}
