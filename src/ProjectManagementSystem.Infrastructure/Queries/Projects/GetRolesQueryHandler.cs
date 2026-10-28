using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetRolesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetRolesQuery, RoleList>
{
    public async Task<RoleList> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectAsync(query.ProjectId, cancellationToken);
        var assigned = project.TeamMembers.Select(m => m.RoleId).ToHashSet();

        var items = project.Roles
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new RoleItem(r.Id, r.Name, r.Description, assigned.Contains(r.Id)))
            .ToList();

        return new RoleList(items, project.Version);
    }
}
