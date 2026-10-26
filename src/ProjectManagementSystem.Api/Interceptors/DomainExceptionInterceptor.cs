using Grpc.Core;
using Grpc.Core.Interceptors;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Api.Interceptors;

/// <summary>
/// Translates the exceptions of a call into gRPC status codes, in one place for every service. A domain error keeps
/// its message, which is meant for the caller and is already logged by the logging behavior of the application. Any
/// other exception is a bug: it is logged here with its details, and the caller gets only a generic message.
/// </summary>
public sealed class DomainExceptionInterceptor(ILogger<DomainExceptionInterceptor> logger) : Interceptor
{
    internal const string InternalErrorMessage = "An internal error occurred.";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (Exception exception) when (exception is not RpcException)
        {
            throw new RpcException(ToStatus(exception, context));
        }
    }

    private Status ToStatus(Exception exception, ServerCallContext context)
    {
        switch (exception)
        {
            case DomainForbiddenException:
                return new Status(StatusCode.PermissionDenied, exception.Message);
            case DomainRuleViolationException:
                return new Status(StatusCode.FailedPrecondition, exception.Message);
            case AggregateNotFoundException or EntityNotFoundException:
                return new Status(StatusCode.NotFound, exception.Message);
            case DomainValidationException:
                return new Status(StatusCode.InvalidArgument, exception.Message);
            case OperationCanceledException when context.CancellationToken.IsCancellationRequested:
                // The caller cancelled or went away; nobody waits for the answer and nothing failed on the server.
                return new Status(StatusCode.Cancelled, "The call was cancelled.");
            default:
                logger.LogError(exception, "Call {Method} failed with an unexpected exception", context.Method);
                return new Status(StatusCode.Internal, InternalErrorMessage);
        }
    }
}
