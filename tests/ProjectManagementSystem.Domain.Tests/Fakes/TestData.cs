using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Tests.Fakes;

public static class TestData
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Project with one role and the given employees in the team since 30 days ago, rate 50.
    /// </summary>
    public static Project ProjectWithTeam(params Guid[] employeeIds)
    {
        var project = new Project(Guid.NewGuid(), "Project", "Description", new Currency("USD"));
        var roleId = project.CreateRole("Developer", "Writes code");

        foreach (var employeeId in employeeIds)
            project.AddTeamMember(employeeId, roleId, Today.AddDays(-30), hourlyRate: 50m);

        return project;
    }

    public static WorkPackage WorkPackage(Project project, Guid responsiblePersonId) =>
        new WorkAssignmentService().CreateWorkPackage(project, "Work package", responsiblePersonId);

    public static ProjectTask Task(Project project, WorkPackage workPackage, Guid responsiblePersonId) =>
        new WorkAssignmentService().AddTask(project, workPackage, "Task", "Description", responsiblePersonId, estimatedEffort: 8m);
}
