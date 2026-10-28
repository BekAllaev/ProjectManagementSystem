namespace ProjectManagementSystem.Infrastructure.ReadModels;

/// <summary>
/// Actual cost of one work package (<c>WorkPackageCostItem</c>).
/// </summary>
public sealed class WorkPackageCostItemView
{
    public Guid WorkPackageId { get; set; }

    public Guid ProjectId { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Hours { get; set; }

    public decimal Cost { get; set; }
}
