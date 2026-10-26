using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;

/// <summary>
/// The employee is only read, to reject an id of an employee that does not exist.
/// </summary>
public sealed class AddTeamMemberCommandHandler(IRepository<Project> projects, IRepository<Employee> employees)
    : ICommandHandler<AddTeamMemberCommand, CreatedId>
{
    public async Task<CreatedId> Handle(AddTeamMemberCommand command, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(command.ProjectId, cancellationToken);
        var employee = await employees.GetAsync(command.EmployeeId, cancellationToken);

        var teamMemberId = project.AddTeamMember(employee.Id, command.RoleId, command.From, command.HourlyRate, command.To);

        projects.Update(project);
        return new CreatedId(teamMemberId);
    }
}
