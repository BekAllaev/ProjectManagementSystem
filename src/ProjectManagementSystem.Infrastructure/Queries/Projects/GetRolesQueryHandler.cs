using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.Projects.Queries.GetRoles;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.Queries.Projects;

internal sealed class GetRolesQueryHandler(AppDbContext dbContext) : IQueryHandler<GetRolesQuery, RoleList>
{
    public async Task<RoleList> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var project = await dbContext.GetProjectItemAsync(query.ProjectId, cancellationToken);
        var roles = await dbContext.RoleItemsOf(project.Id).ToListAsync(cancellationToken);

        return new RoleList(
            roles.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(r => new RoleItem(r.Id, r.Name, r.Description, r.IsAssigned))
                .ToList(),
            project.Version);
    }
}
