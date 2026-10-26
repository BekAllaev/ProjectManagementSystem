using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Funding;
using ProjectManagementSystem.Domain.Projects;

namespace ProjectManagementSystem.Domain.Services;

/// <summary>
/// Adds funding to a project. A closed project gets no new funding; the state lives in
/// <see cref="Project"/>, so the rule spans aggregates. The project may be closed concurrently; this is
/// accepted without compensation, at worst a superfluous tranche appears. Only
/// <see cref="ProjectFunding"/> is changed.
/// </summary>
public sealed class FundingService
{
    /// <summary>
    /// Creates the funding of a project before its first tranche.
    /// </summary>
    /// <returns>New funding; the caller adds it to the repository.</returns>
    public ProjectFunding CreateFunding(Project project) => new(project.Id);

    public void AddFunding(Project project, ProjectFunding funding, decimal amount, DateOnly date, string reason)
    {
        if (funding.Id != project.Id)
            throw new DomainValidationException($"Funding '{funding.Id}' does not belong to project '{project.Id}'.");

        if (!project.IsOpen)
            throw new DomainRuleViolationException($"Project '{project.Id}' is {project.State}; it gets no new funding.");

        funding.AddFunding(amount, date, reason);
    }
}
