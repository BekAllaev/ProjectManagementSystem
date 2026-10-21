using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Services;
using ProjectManagementSystem.Domain.WorkPackages.Events;
using ProjectManagementSystem.Domain.Tests.Fakes;

namespace ProjectManagementSystem.Domain.Tests;

public class WorkPackageServiceTests
{
    private readonly WorkPackageService _service = new(TestData.TimeProvider);
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();

    [Fact]
    public void Responsible_person_must_be_a_team_member()
    {
        var project = TestData.ProjectWithTeam(_member);

        Assert.Throws<DomainException>(() => _service.CreateWorkPackage(project, "Package", Guid.NewGuid()));

        var workPackage = _service.CreateWorkPackage(project, "Package", _member);
        Assert.Throws<DomainException>(() => _service.AddTask(project, workPackage, "Task", "", Guid.NewGuid(), 1m));
        Assert.Throws<DomainException>(() => _service.ChangeWorkPackageResponsiblePerson(project, workPackage, Guid.NewGuid()));
    }

    [Fact]
    public void Responsible_person_of_a_task_can_be_changed_to_another_member()
    {
        var project = TestData.ProjectWithTeam(_member, _other);
        var workPackage = TestData.WorkPackage(project, _member);
        var task = TestData.Task(project, workPackage, _member);

        _service.ChangeTaskResponsiblePerson(project, workPackage, task.Id, _other);

        Assert.Equal(_other, task.ResponsiblePersonId);
        Assert.Contains(workPackage.DomainEvents, e => e is ResponsiblePersonChanged changed && changed.TaskId == task.Id);
    }

    [Fact]
    public void Work_package_of_another_project_is_rejected()
    {
        var project = TestData.ProjectWithTeam(_member);
        var otherProject = TestData.ProjectWithTeam(_member);
        var workPackage = TestData.WorkPackage(otherProject, _member);

        Assert.Throws<DomainException>(() => _service.AddTask(project, workPackage, "Task", "", _member, 1m));
    }

    [Fact]
    public void New_work_package_is_registered_in_the_open_project()
    {
        var project = TestData.ProjectWithTeam(_member);

        var workPackage = _service.CreateWorkPackage(project, "Package", _member);

        Assert.Equal([workPackage.Id], project.WorkPackageIds);

        project.Start();
        project.Complete(DateTime.UtcNow);

        Assert.Throws<DomainException>(() => _service.CreateWorkPackage(project, "Package", _member));
        Assert.Single(project.WorkPackageIds);
    }
}
