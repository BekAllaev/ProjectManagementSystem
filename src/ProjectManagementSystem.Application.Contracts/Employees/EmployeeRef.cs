using System.Runtime.Serialization;

namespace ProjectManagementSystem.Application.Employees;

/// <summary>
/// An employee as other read models show them. Names are empty if the employee does not exist.
/// </summary>
[DataContract]
public sealed record EmployeeRef(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string FirstName,
    [property: DataMember(Order = 3)] string LastName)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private EmployeeRef() : this(default, default!, default!)
    {
    }
}
