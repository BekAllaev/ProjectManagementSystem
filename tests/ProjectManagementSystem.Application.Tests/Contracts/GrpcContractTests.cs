using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Grpc.Core;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Meta;

namespace ProjectManagementSystem.Application.Tests.Contracts;

/// <summary>
/// Commands and their results are gRPC messages and the service interfaces are gRPC services (protobuf-net.Grpc,
/// code-first). These tests catch a contract that would fail only on the wire.
/// </summary>
public class GrpcContractTests
{
    private static readonly Assembly ContractsAssembly = typeof(ICommand).Assembly;

    public static TheoryData<Type> Messages() => new(MessageTypes());

    public static TheoryData<Type> Services() => new(ServiceTypes());

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_is_a_data_contract_with_a_unique_order_for_every_property(Type type)
    {
        Assert.NotNull(type.GetCustomAttribute<DataContractAttribute>());

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == type && p.Name != "EqualityContract")
            .ToList();
        var orders = properties.Select(p => p.GetCustomAttribute<DataMemberAttribute>()?.Order ?? 0).ToList();

        Assert.All(orders, order => Assert.True(order > 0));
        Assert.Equal(orders.Count, orders.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_survives_serialization_with_every_value_set(Type type) =>
        AssertRoundTrip(Create(type, nullableAsNull: false));

    [Theory]
    [MemberData(nameof(Messages))]
    public void Message_survives_serialization_with_empty_optional_values(Type type) =>
        AssertRoundTrip(Create(type, nullableAsNull: true));

    [Theory]
    [MemberData(nameof(Services))]
    public void Service_can_be_called_through_a_client(Type serviceType)
    {
        // Creating the client checks every method: a request or response that is not a message fails here.
        var createClient = typeof(GrpcClientFactory)
            .GetMethod(nameof(GrpcClientFactory.CreateGrpcService), [typeof(CallInvoker), typeof(ProtoBuf.Grpc.Configuration.ClientFactory)])!
            .MakeGenericMethod(serviceType);

        Assert.NotNull(createClient.Invoke(null, [new UnusedCallInvoker(), null]));
    }

    [Fact]
    public void Every_command_has_exactly_one_service_method()
    {
        var commands = MessageTypes().Where(t => t != typeof(CreatedId));
        var requests = ServiceTypes()
            .SelectMany(s => s.GetMethods())
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToList();

        Assert.All(commands, command => Assert.Single(requests, r => r == command));
    }

    private static IEnumerable<Type> MessageTypes() =>
        ContractsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IBaseCommand).IsAssignableFrom(t))
            .Append(typeof(CreatedId))
            .OrderBy(t => t.Name);

    private static IEnumerable<Type> ServiceTypes() =>
        ContractsAssembly.GetTypes().Where(t => t.GetCustomAttribute<ServiceContractAttribute>() is not null);

    private static void AssertRoundTrip(object message)
    {
        using var stream = new MemoryStream();
        RuntimeTypeModel.Default.Serialize(stream, message);
        stream.Position = 0;

        var copy = RuntimeTypeModel.Default.Deserialize(stream, null, message.GetType());

        Assert.Equal(message, copy);
    }

    private static object Create(Type type, bool nullableAsNull)
    {
        var constructor = type.GetConstructors().Single(c => c.GetParameters() is not [{ } p] || p.ParameterType != type);
        var context = new NullabilityInfoContext();
        var arguments = constructor.GetParameters()
            .Select(p => nullableAsNull && context.Create(p).WriteState == NullabilityState.Nullable ? null : Sample(p.ParameterType))
            .ToArray();

        return constructor.Invoke(arguments);
    }

    private static object Sample(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return Enum.GetValues(type).Cast<object>().Last();

        return type switch
        {
            _ when type == typeof(Guid) => Guid.NewGuid(),
            _ when type == typeof(string) => "Text",
            _ when type == typeof(DateOnly) => new DateOnly(2026, 3, 2),
            _ when type == typeof(decimal) => 1234.56m,
            _ => throw new NotSupportedException($"No sample value for {type}."),
        };
    }

    private sealed class UnusedCallInvoker : CallInvoker
    {
        public override TResponse BlockingUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
            throw new NotSupportedException();

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
            throw new NotSupportedException();

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options, TRequest request) =>
            throw new NotSupportedException();

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options) =>
            throw new NotSupportedException();

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
            Method<TRequest, TResponse> method, string? host, CallOptions options) =>
            throw new NotSupportedException();
    }
}
