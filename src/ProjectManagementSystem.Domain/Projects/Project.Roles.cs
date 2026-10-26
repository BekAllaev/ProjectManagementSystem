using ProjectManagementSystem.Domain.Base;
using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Events;
using ProjectManagementSystem.Domain.Projects.Roles;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<Role> _roles = [];

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    public Guid CreateRole(string name, string description)
    {
        name = Guard.NotEmpty(name, "Role name");
        EnsureRoleNameIsUnique(name);

        var role = new Role(Guid.NewGuid(), name, description);
        _roles.Add(role);

        RaiseDomainEvent(new RoleCreated(Id, role.Id, role.Name, role.Description));
        return role.Id;
    }

    public void RenameRole(Guid roleId, string newName)
    {
        var role = GetRole(roleId);
        newName = Guard.NotEmpty(newName, "Role name");

        if (string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
            return;

        EnsureRoleWasNeverAssigned(role, "renamed");
        EnsureRoleNameIsUnique(newName);

        role.Rename(newName);
        RaiseDomainEvent(new RoleRenamed(Id, role.Id, role.Name));
    }

    public void ChangeRoleDescription(Guid roleId, string description)
    {
        var role = GetRole(roleId);

        role.ChangeDescription(description);
        RaiseDomainEvent(new RoleDescriptionChanged(Id, role.Id, role.Description));
    }

    public void DeleteRole(Guid roleId)
    {
        var role = GetRole(roleId);
        EnsureRoleWasNeverAssigned(role, "deleted");

        _roles.Remove(role);
        RaiseDomainEvent(new RoleDeleted(Id, role.Id));
    }

    private Role GetRole(Guid roleId) =>
        _roles.SingleOrDefault(r => r.Id == roleId) ?? throw new EntityNotFoundException(nameof(Role), roleId);

    private void EnsureRoleNameIsUnique(string name)
    {
        if (_roles.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainRuleViolationException($"Role with name '{name}' already exists in the project.");
    }

    private void EnsureRoleWasNeverAssigned(Role role, string action)
    {
        if (IsRoleEverAssigned(role.Id))
            throw new DomainRuleViolationException($"Role '{role.Name}' cannot be {action} because it has been assigned to a team member.");
    }
}
