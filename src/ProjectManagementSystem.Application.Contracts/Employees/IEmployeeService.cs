using System.ServiceModel;
using ProtoBuf.Grpc;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;
using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;
using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;

namespace ProjectManagementSystem.Application.Employees;

/// <summary>
/// gRPC service of the Employee aggregate: one method per command. The host implements every
/// method by sending the command through MediatR; the client shares this interface for typed calls.
/// </summary>
[ServiceContract(Name = "ProjectManagementSystem.EmployeeService")]
public interface IEmployeeService
{
    Task ChangeEmployeeEmail(ChangeEmployeeEmailCommand command, CallContext context = default);

    Task ChangeEmployeePhoto(ChangeEmployeePhotoCommand command, CallContext context = default);

    Task<CreatedId> RegisterEmployee(RegisterEmployeeCommand command, CallContext context = default);

    Task RenameEmployee(RenameEmployeeCommand command, CallContext context = default);
}
