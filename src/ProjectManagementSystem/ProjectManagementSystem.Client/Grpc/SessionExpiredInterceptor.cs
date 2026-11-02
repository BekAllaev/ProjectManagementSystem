using Grpc.Core;
using Grpc.Core.Interceptors;
using ProjectManagementSystem.Client.Auth;

namespace ProjectManagementSystem.Client.Grpc;

/// <summary>
/// Signs the user out when the API rejects their token, so the router sends them to the login page instead of every
/// view showing its own error. The call still fails with its <see cref="RpcException"/>.
/// </summary>
internal sealed class SessionExpiredInterceptor(AuthService auth) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            ObserveAsync(call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> ObserveAsync<TResponse>(Task<TResponse> response)
    {
        try
        {
            return await response;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.Unauthenticated)
        {
            await auth.OnSessionRejectedAsync();
            throw;
        }
    }
}
