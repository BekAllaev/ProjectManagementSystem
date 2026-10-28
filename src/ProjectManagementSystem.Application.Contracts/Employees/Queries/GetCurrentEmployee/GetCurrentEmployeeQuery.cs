using System.Runtime.Serialization;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;

/// <summary>
/// The employee of the caller, for the header of the client.
/// </summary>
[DataContract]
public sealed record GetCurrentEmployeeQuery : IQuery<CurrentEmployee>;
