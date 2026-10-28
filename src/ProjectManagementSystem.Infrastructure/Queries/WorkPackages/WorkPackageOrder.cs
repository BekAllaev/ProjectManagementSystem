using ProjectManagementSystem.Application.ReadModels;

namespace ProjectManagementSystem.Infrastructure.Queries.WorkPackages;

internal static class WorkPackageOrder
{
    /// <summary>
    /// In the order of registration in the project; a package not registered yet comes last, by creation.
    /// </summary>
    public static IOrderedEnumerable<WorkPackageItemView> InProjectOrder(this IEnumerable<WorkPackageItemView> workPackages) =>
        workPackages.OrderBy(w => w.RegistrationPosition ?? int.MaxValue).ThenBy(w => w.CreatedAt);
}
