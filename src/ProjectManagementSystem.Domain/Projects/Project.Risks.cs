using ProjectManagementSystem.Domain.Exceptions;
using ProjectManagementSystem.Domain.Projects.Risks;

namespace ProjectManagementSystem.Domain.Projects;

public sealed partial class Project
{
    private readonly List<Risk> _risks = [];

    public IReadOnlyCollection<Risk> Risks => _risks.AsReadOnly();

    public Guid AddRisk(string description, RiskLevel probability, RiskLevel impact, Guid ownerId, RiskStatus status = RiskStatus.Open)
    {
        var risk = new Risk(Guid.NewGuid(), description, probability, impact, ownerId, status);
        _risks.Add(risk);
        return risk.Id;
    }

    public Risk GetRisk(Guid riskId) =>
        _risks.SingleOrDefault(r => r.Id == riskId) ?? throw new EntityNotFoundException(nameof(Risk), riskId);

    public void RemoveRisk(Guid riskId) => _risks.Remove(GetRisk(riskId));
}
