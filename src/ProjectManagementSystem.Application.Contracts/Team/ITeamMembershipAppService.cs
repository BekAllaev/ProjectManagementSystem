namespace ProjectManagementSystem.Application.Contracts.Team;

public interface ITeamMembershipAppService
{
    Task EndTeamMembershipAsync(Guid projectId, Guid employeeId, DateOnly to, CancellationToken cancellationToken = default);
}
