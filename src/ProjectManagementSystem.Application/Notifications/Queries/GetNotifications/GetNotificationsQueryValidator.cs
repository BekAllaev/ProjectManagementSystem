using FluentValidation;

namespace ProjectManagementSystem.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(q => q.Take).GreaterThan(0);
    }
}
