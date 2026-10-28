using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.ReadModels;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Tests.Persistence;

public sealed class ReadModelRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task Reads_see_the_unsaved_changes_of_the_scope_and_the_saved_rows()
    {
        var saved = new ProjectItemView { Id = Guid.NewGuid(), Title = "Saved", State = ProjectState.Created };
        var removed = new ProjectItemView { Id = Guid.NewGuid(), Title = "Removed", State = ProjectState.Created };
        await InScopeAsync(async services =>
        {
            var projects = services.GetRequiredService<IReadModelRepository<ProjectItemView>>();
            projects.Add(saved);
            projects.Add(removed);
            await services.GetRequiredService<AppDbContext>().SaveChangesAsync();
        });

        await InScopeAsync(async services =>
        {
            var projects = services.GetRequiredService<IReadModelRepository<ProjectItemView>>();
            projects.Add(new ProjectItemView { Id = Guid.NewGuid(), Title = "Added", State = ProjectState.Created });
            projects.Remove((await projects.FindAsync(p => p.Id == removed.Id))!);
            (await projects.FindAsync(p => p.Id == saved.Id))!.State = ProjectState.InProgress;

            var all = await projects.ListAsync(_ => true);
            var started = await projects.ListAsync(p => p.State == ProjectState.InProgress);

            Assert.Equal(["Added", "Saved"], all.Select(p => p.Title).Order());
            Assert.Equal(["Saved"], started.Select(p => p.Title));
        });
    }
}
