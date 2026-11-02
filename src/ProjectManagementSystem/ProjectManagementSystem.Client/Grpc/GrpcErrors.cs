using Grpc.Core;

namespace ProjectManagementSystem.Client.Grpc;

/// <summary>
/// Text for the user about a failed gRPC call. The API puts a readable message in the detail of the status for the
/// failures of the domain (forbidden, rule violated, not found, invalid, concurrency conflict).
/// </summary>
public static class GrpcErrors
{
    public static string Describe(Exception exception) => exception switch
    {
        // The browser could not send the call at all (the API is down, or CORS refused it): gRPC reports that as
        // Internal with the HttpRequestException of fetch in its debug exception.
        RpcException rpc when rpc.StatusCode == StatusCode.Unavailable || rpc.Status.DebugException is HttpRequestException =>
            "The server cannot be reached. Check that the API is running and try again.",
        RpcException { StatusCode: StatusCode.Unauthenticated } =>
            "Your session has ended. Please sign in again.",
        RpcException { StatusCode: StatusCode.Internal or StatusCode.Unknown } rpc when string.IsNullOrWhiteSpace(rpc.Status.Detail) =>
            "Something went wrong on the server.",
        RpcException rpc when !string.IsNullOrWhiteSpace(rpc.Status.Detail) => rpc.Status.Detail,
        RpcException rpc => $"The server answered {rpc.StatusCode}.",
        _ => "Something went wrong.",
    };
}
