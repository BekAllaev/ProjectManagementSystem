using MediatR;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

/// <summary>
/// Reads data and changes nothing. Queries and read models come later.
/// </summary>
public interface IQuery<out TResult> : IRequest<TResult>
{
}

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
}
