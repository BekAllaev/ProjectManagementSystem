using ProjectManagementSystem.Domain.Base;

namespace ProjectManagementSystem.Domain.Projects.Roles;

/// <summary>
/// Role inside a project. Changed only through <see cref="Project"/>, because the rules
/// for roles depend on the team members of the project.
/// </summary>
public sealed class Role : Entity<Guid>
{
    public string Name { get; private set; }

    public string Description { get; private set; }

    internal Role(Guid id, string name, string description) : base(id)
    {
        Name = Guard.NotEmpty(name, nameof(Name));
        Description = description ?? string.Empty;
    }

    internal void Rename(string newName) => Name = Guard.NotEmpty(newName, nameof(Name));

    internal void ChangeDescription(string description) => Description = description ?? string.Empty;
}
