using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.WorkPackages;

/// <summary>
/// Number of tasks in every status.
/// </summary>
[DataContract]
public sealed record TaskStatusCounts(
    [property: DataMember(Order = 1)] int Open,
    [property: DataMember(Order = 2)] int InProgress,
    [property: DataMember(Order = 3)] int Completed,
    [property: DataMember(Order = 4)] int Cancelled)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private TaskStatusCounts() : this(default, default, default, default)
    {
    }
}
