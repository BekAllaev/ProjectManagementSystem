using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;

/// <summary>
/// New hourly rate of the employee starting on <paramref name="From"/> (today or later).
/// </summary>
[DataContract]
public sealed record ChangeHourlyRateCommand(
    [property: DataMember(Order = 1)] Guid ProjectId,
    [property: DataMember(Order = 2)] Guid EmployeeId,
    [property: DataMember(Order = 3)] decimal Amount,
    [property: DataMember(Order = 4)] DateOnly From) : ICommand
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private ChangeHourlyRateCommand() : this(default, default, default, default)
    {
    }
}
