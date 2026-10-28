using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeeEmail;
using ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;
using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Employees.Commands.RenameEmployee;
using ProjectManagementSystem.Application.Employees.Queries.GetCurrentEmployee;
using ProjectManagementSystem.Application.Employees.Queries.GetEmployees;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Services;

/// <summary>
/// Sends every command and query of <see cref="IEmployeeService"/> through MediatR, so the pipeline behaviors apply.
/// No logic here.
/// </summary>
[Authorize]
public sealed class EmployeeGrpcService(ISender mediator) : IEmployeeService
{
    public Task ChangeEmployeeEmail(ChangeEmployeeEmailCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task ChangeEmployeePhoto(ChangeEmployeePhotoCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task<CurrentEmployee> GetCurrentEmployee(GetCurrentEmployeeQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task<EmployeeList> GetEmployees(GetEmployeesQuery query, CallContext context = default) =>
        mediator.Send(query, context.CancellationToken);

    public Task<CreatedId> RegisterEmployee(RegisterEmployeeCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);

    public Task RenameEmployee(RenameEmployeeCommand command, CallContext context = default) =>
        mediator.Send(command, context.CancellationToken);
}
