using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Application.Projects.Queries.GetRoles;

public sealed class GetRolesQueryHandler(
    IReadModelRepository<ProjectItemView> projects,
    IReadModelRepository<RoleItemView> roles) : IQueryHandler<GetRolesQuery, RoleList>
{
    public async Task<RoleList> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var project = await projects.GetAsync(query.ProjectId, cancellationToken);
        var rows = await roles.ListAsync(r => r.ProjectId == project.Id, cancellationToken);

        return new RoleList(
            rows.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(r => new RoleItem(r.Id, r.Name, r.Description, r.IsAssigned))
                .ToList(),
            project.Version);
    }
}
