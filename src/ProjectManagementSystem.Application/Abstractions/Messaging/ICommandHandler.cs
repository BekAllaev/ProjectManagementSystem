using MediatR;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

/// <summary>
/// Exactly one handler per command. It loads the aggregates, calls a domain service or one domain method
/// and returns; it never saves (see <see cref="Behaviors.UnitOfWorkBehavior{TRequest, TResponse}"/>).
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Unit>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
}
