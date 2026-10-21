using MediatR;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
}
