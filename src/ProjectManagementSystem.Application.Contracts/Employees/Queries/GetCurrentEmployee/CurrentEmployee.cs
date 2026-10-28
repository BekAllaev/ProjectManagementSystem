using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;

/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record CurrentEmployee(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string FirstName,
    [property: DataMember(Order = 3)] string LastName,
    [property: DataMember(Order = 4)] string Email,
    [property: DataMember(Order = 5)] string? PhotoPath,
    [property: DataMember(Order = 6)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private CurrentEmployee() : this(default, default!, default!, default!, default, default)
    {
    }
}
