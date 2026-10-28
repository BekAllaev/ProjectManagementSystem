namespace ProjectManagementSystem.Application.ReadModels;

/// <summary>
/// Actual cost of one team member in a project (<c>MemberCostItem</c>).
/// </summary>
public sealed class MemberCostItemView
{
    public Guid ProjectId { get; set; }

    public Guid EmployeeId { get; set; }

    public decimal Hours { get; set; }

    public decimal Cost { get; set; }
}
