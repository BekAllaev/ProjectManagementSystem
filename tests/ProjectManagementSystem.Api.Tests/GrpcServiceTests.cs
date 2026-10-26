using System.Reflection;
using System.Runtime.CompilerServices;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProjectManagementSystem.Api.Services;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProtoBuf.Grpc;

namespace ProjectManagementSystem.Api.Tests;

/// <summary>
/// Every service method only sends its command through MediatR with the cancellation of the call.
/// </summary>
public class GrpcServiceTests
{
    public static TheoryData<Type> Services() =>
        new(typeof(ProjectGrpcService).Assembly.GetTypes().Where(t => t.Namespace == typeof(ProjectGrpcService).Namespace && t.IsClass));

    [Theory]
    [MemberData(nameof(Services))]
    public void Service_requires_an_authenticated_caller(Type service) =>
        Assert.NotNull(service.GetCustomAttribute<AuthorizeAttribute>());

    [Theory]
    [MemberData(nameof(Services))]
    public async Task Every_method_sends_its_command_with_the_cancellation_of_the_call(Type service)
    {
        var sender = new RecordingSender();
        var instance = Activator.CreateInstance(service, sender)!;
        using var cancellation = new CancellationTokenSource();

        foreach (var method in service.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var command = RuntimeHelpers.GetUninitializedObject(method.GetParameters()[0].ParameterType);

            await (Task)method.Invoke(instance, [command, new CallContext(new CallOptions(cancellationToken: cancellation.Token))])!;

            Assert.Same(command, sender.Request);
            Assert.Equal(cancellation.Token, sender.CancellationToken);
        }
    }

    private sealed class RecordingSender : ISender
    {
        public object? Request { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Request = request;
            CancellationToken = cancellationToken;

            object response = typeof(TResponse) == typeof(CreatedId) ? new CreatedId(Guid.NewGuid()) : Unit.Value;
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
