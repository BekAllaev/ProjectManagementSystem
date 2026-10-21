using MediatR;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Application.Abstractions.Messaging;

namespace ProjectManagementSystem.Application.Behaviors;

/// <summary>
/// Innermost behavior, for commands only: runs the handler and then saves the unit of work exactly once.
/// If the handler throws, nothing is saved. Handlers never call <see cref="IUnitOfWork.SaveChangesAsync"/>.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IBaseCommand)
            return await next(cancellationToken);

        var response = await next(cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
