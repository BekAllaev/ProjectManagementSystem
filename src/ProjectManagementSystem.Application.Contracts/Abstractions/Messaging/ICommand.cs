using MediatR;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

/// <summary>
/// Marker of every command, so a pipeline behavior can tell commands from queries.
/// A command changes exactly one aggregate; the unit of work is saved once after its handler.
/// </summary>
public interface IBaseCommand
{
}

public interface ICommand : IRequest<Unit>, IBaseCommand
{
}

/// <summary>
/// Command with a result, for example <see cref="CreatedId"/>. The result is a gRPC message (<c>[DataContract]</c>);
/// a command without a result answers with an empty message.
/// </summary>
public interface ICommand<out TResult> : IRequest<TResult>, IBaseCommand
{
}
