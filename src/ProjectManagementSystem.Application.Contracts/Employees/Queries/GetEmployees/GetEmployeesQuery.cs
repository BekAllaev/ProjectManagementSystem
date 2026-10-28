using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Queries.GetEmployees;

/// <summary>
/// Employees ordered by last and first name.
/// </summary>
/// <param name="Search">
/// Only employees whose name or email contains this text (case-insensitive); empty for all.
/// </param>
[DataContract]
public sealed record GetEmployeesQuery(
    [property: DataMember(Order = 1)] string? Search) : IQuery<EmployeeList>
{
    /// <summary>
    /// For the serializer of gRPC only.
    /// </summary>
    private GetEmployeesQuery() : this(default(string?))
    {
    }
}
