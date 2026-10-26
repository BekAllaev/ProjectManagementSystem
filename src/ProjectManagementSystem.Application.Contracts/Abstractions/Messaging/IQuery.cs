using MediatR;

namespace ProjectManagementSystem.Application.Abstractions.Messaging;

/// <summary>
/// Reads data and changes nothing. Queries and read models come later. Like a command, a query is a gRPC message
/// (<c>[DataContract]</c>), and so is its result: a read model, or a response record with an <c>Items</c> list for
/// a list, never a bare collection or primitive.
/// </summary>
public interface IQuery<out TResult> : IRequest<TResult>
{
}
