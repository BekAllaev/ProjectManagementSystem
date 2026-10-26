using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Risks;

namespace ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;

/// <summary>
/// Replaces all editable fields of the risk (plain CRUD).
/// </summary>
[DataContract]
public sealed record UpdateRiskCommand(
    [property: DataMember(Order = 1)] Guid RiskId,
    [property: DataMember(Order = 2)] string Description,
    [property: DataMember(Order = 3)] RiskLevel Probability,
    [property: DataMember(Order = 4)] RiskLevel Impact,
    [property: DataMember(Order = 5)] Guid OwnerId,
    [property: DataMember(Order = 6)] RiskStatus Status) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private UpdateRiskCommand() : this(default, default!, default, default, default, default)
    {
    }
}
