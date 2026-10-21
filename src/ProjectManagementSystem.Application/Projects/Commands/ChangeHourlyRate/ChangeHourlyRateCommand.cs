using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;

/// <summary>
/// New hourly rate of the employee starting on <paramref name="From"/> (today or later).
/// </summary>
public sealed record ChangeHourlyRateCommand(Guid ProjectId, Guid EmployeeId, decimal Amount, DateOnly From) : ICommand;
