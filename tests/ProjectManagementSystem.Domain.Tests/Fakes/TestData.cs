using Microsoft.Extensions.Time.Testing;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages;

namespace ProjectManagementSystem.Domain.Tests.Fakes;

public static class TestData
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Time provider for domain services, set to the start of <see cref="Today"/>.
    /// </summary>
    public static TimeProvider TimeProvider =>
        new FakeTimeProvider(new DateTimeOffset(Today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

    /// <summary>
    /// Project with one role and the given employees in the team since 30 days ago, rate 50.
    /// </summary>
    public static Project ProjectWithTeam(params Guid[] employeeIds)
    {
        var project = new Project(Guid.NewGuid(), "Project", "Description", new Currency("USD"), DateTime.UtcNow);
        var roleId = project.CreateRole("Developer", "Writes code");

        foreach (var employeeId in employeeIds)
            project.AddTeamMember(employeeId, roleId, Today.AddDays(-30), hourlyRate: 50m);

        return project;
    }

    /// <summary>
    /// Starts and completes the project, bypassing <see cref="ProjectService.Complete"/>.
    /// </summary>
    public static void Close(Project project)
    {
        project.Start(DateTime.UtcNow);
        project.Complete(DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a work package and registers it in the project, as the handler of
    /// <see cref="WorkPackages.Events.WorkPackageCreated"/> does after the commit.
    /// </summary>
    public static WorkPackage WorkPackage(Project project, Guid responsiblePersonId)
    {
        var workPackage = new WorkPackageService(TimeProvider).CreateWorkPackage(project, "Work package", responsiblePersonId);
        project.RegisterWorkPackage(workPackage.Id);

        return workPackage;
    }

    public static ProjectTask Task(Project project, WorkPackage workPackage, Guid responsiblePersonId) =>
        new WorkPackageService(TimeProvider).AddTask(project, workPackage, "Task", "Description", responsiblePersonId, estimatedEffort: 8m);
}
