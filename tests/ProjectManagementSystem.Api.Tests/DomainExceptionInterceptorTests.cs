using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectManagementSystem.Api.Interceptors;
using ProjectManagementSystem.Application.Exceptions;
using ProjectManagementSystem.Domain.Exceptions;

namespace ProjectManagementSystem.Api.Tests;

public class DomainExceptionInterceptorTests
{
    private readonly DomainExceptionInterceptor _interceptor = new(NullLogger<DomainExceptionInterceptor>.Instance);

    public static TheoryData<Exception, StatusCode> DomainErrors() => new()
    {
        { new DomainForbiddenException("Not yours."), StatusCode.PermissionDenied },
        { new DomainRuleViolationException("Already closed."), StatusCode.FailedPrecondition },
        { new AggregateNotFoundException("Project", Guid.NewGuid()), StatusCode.NotFound },
        { new EntityNotFoundException("Task", Guid.NewGuid()), StatusCode.NotFound },
        { new DomainValidationException("Name must not be empty."), StatusCode.InvalidArgument },
    };

    [Theory]
    [MemberData(nameof(DomainErrors))]
    public async Task Domain_error_becomes_its_status_with_its_message(Exception error, StatusCode expected)
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() => CallFailingWith(error));

        Assert.Equal(expected, exception.StatusCode);
        Assert.Equal(error.Message, exception.Status.Detail);
    }

    [Fact]
    public async Task Unexpected_error_becomes_internal_without_its_details()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            CallFailingWith(new InvalidOperationException("Connection string of the secret server")));

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
        Assert.Equal(DomainExceptionInterceptor.InternalErrorMessage, exception.Status.Detail);
    }

    [Fact]
    public async Task Cancellation_by_the_caller_becomes_cancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            CallFailingWith(new OperationCanceledException(cancellation.Token), new FakeServerCallContext(cancellation.Token)));

        Assert.Equal(StatusCode.Cancelled, exception.StatusCode);
    }

    [Fact]
    public async Task Rpc_exception_passes_unchanged()
    {
        var error = new RpcException(new Status(StatusCode.Unavailable, "Later."));

        Assert.Same(error, await Assert.ThrowsAsync<RpcException>(() => CallFailingWith(error)));
    }

    [Fact]
    public async Task Successful_call_returns_its_response()
    {
        var response = await _interceptor.UnaryServerHandler<string, string>(
            "request", new FakeServerCallContext(), (request, _) => Task.FromResult(request + " handled"));

        Assert.Equal("request handled", response);
    }

    private Task<string> CallFailingWith(Exception error, ServerCallContext? context = null) =>
        _interceptor.UnaryServerHandler<string, string>("request", context ?? new FakeServerCallContext(), (_, _) => throw error);

    private sealed class FakeServerCallContext(CancellationToken cancellationToken = default) : ServerCallContext
    {
        protected override string MethodCore => "/Test/Method";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "test";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
