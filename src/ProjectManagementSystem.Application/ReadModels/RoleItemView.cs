namespace ProjectManagementSystem.Application.ReadModels;

/// <summary>
/// Row of the role catalog of a project (<c>RoleItem</c>).
/// </summary>
public sealed class RoleItemView
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Assigned to a team member now or before; then it can no longer be renamed or deleted.
    /// </summary>
    public bool IsAssigned { get; set; }
}
