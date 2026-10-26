using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Funding.Commands.AddFunding;

/// <summary>
/// Adds a tranche of funding to the project. <paramref name="Amount"/> is in the currency of the project.
/// </summary>
[DataContract]
public sealed record AddFundingCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] decimal Amount,
    [property: DataMember(Order = 3)] DateOnly Date,
    [property: DataMember(Order = 4)] string? Reason) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private AddFundingCommand() : this(default, default, default, default)
    {
    }
}
