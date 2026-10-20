using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Milestones;

public interface IMilestoneRepository : IRepository<Milestone>
{
    void Remove(Milestone milestone);
}
