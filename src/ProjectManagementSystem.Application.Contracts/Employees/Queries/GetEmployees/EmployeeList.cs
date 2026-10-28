using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Projects;

namespace ProjectManagementSystem.Application.Employees.Queries.GetEmployees;

[DataContract]
public sealed record EmployeeList(
    [property: DataMember(Order = 1)] IReadOnlyList<EmployeeItem> Items)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private EmployeeList() : this(new List<EmployeeItem>())
    {
    }
}

/// <param name="CurrentProjects">Projects whose team the employee is a member of today.</param>
/// <param name="Version">Version of the aggregate it is read from (see the read side in docs/domain-model.md).</param>
[DataContract]
public sealed record EmployeeItem(
    [property: DataMember(Order = 1)] Guid Id,
    [property: DataMember(Order = 2)] string FirstName,
    [property: DataMember(Order = 3)] string LastName,
    [property: DataMember(Order = 4)] string Email,
    [property: DataMember(Order = 5)] string? PhotoPath,
    [property: DataMember(Order = 6)] IReadOnlyList<ProjectRef> CurrentProjects,
    [property: DataMember(Order = 7)] long Version)
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private EmployeeItem() : this(default, default!, default!, default!, default, new List<ProjectRef>(), default)
    {
    }
}
