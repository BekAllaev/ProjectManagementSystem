using FluentValidation;

namespace ProjectManagementSystem.Application.Employees.Commands.ChangeEmployeePhoto;

public sealed class ChangeEmployeePhotoCommandValidator : AbstractValidator<ChangeEmployeePhotoCommand>
{
    public ChangeEmployeePhotoCommandValidator()
    {
        RuleFor(c => c.EmployeeId).NotEmpty();
        RuleFor(c => c.PhotoPath).MaximumLength(500);
    }
}
