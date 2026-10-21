using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;

/// <param name="PhotoPath">Empty removes the photo.</param>
public sealed record ChangeEmployeePhotoCommand(Guid EmployeeId, string? PhotoPath) : ICommand;
