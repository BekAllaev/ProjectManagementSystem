using ProjectManagementSystem.Domain.Interfaces;

namespace ProjectManagementSystem.Domain.Risks;

public interface IRiskRepository : IRepository<Risk>
{
    void Remove(Risk risk);
}
