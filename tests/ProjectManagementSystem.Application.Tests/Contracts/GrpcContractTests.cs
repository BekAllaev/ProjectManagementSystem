using System.Reflection;
using System.Runtime.Serialization;
using System.ServiceModel;
using Grpc.Core;
using ProjectManagementSystem.Application.Abstractions.Messaging;
using ProtoBuf.Grpc.Client;
using ProtoBuf.Meta;

namespace ProjectManagementSystem.Application.Tests.Contracts;

/// <summary>
/// Commands, queries and their results are gRPC messages and the service interfaces are gRPC services (protobuf-net.Grpc,
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
    public void Every_command_and_query_has_exactly_one_service_method()
    {
        var requests = ServiceTypes()
            .SelectMany(s => s.GetMethods())
            .Select(m => m.GetParameters()[0].ParameterType)
            .ToList();

        Assert.All(RequestTypes(), request => Assert.Single(requests, r => r == request));
    }

    [Fact]
    public void Every_message_is_a_command_a_query_or_a_result_of_one()
    {
        var results = ServiceTypes()
            .SelectMany(s => s.GetMethods())
            .Select(m => m.ReturnType)
            .Where(t => t.IsGenericType)
            .Select(t => t.GetGenericArguments()[0]);
        var reachable = new HashSet<Type>(RequestTypes().Concat(results).SelectMany(PartsOf));

        Assert.All(MessageTypes(), message => Assert.Contains(message, reachable));
    }

    /// <summary>
    /// Every type with <c>[DataContract]</c>: commands, queries, their results and the read models inside them.
    /// </summary>
    private static IEnumerable<Type> MessageTypes() =>
        ContractsAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<DataContractAttribute>() is not null)
            .OrderBy(t => t.FullName);

    private static IEnumerable<Type> RequestTypes() =>
        MessageTypes().Where(t => typeof(IBaseCommand).IsAssignableFrom(t) || IsQuery(t));

    private static bool IsQuery(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

    /// <summary>
    /// The message and every message it contains, directly or in a list.
    /// </summary>
    private static IEnumerable<Type> PartsOf(Type type)
    {
        var element = ElementType(type) ?? type;
        if (element.GetCustomAttribute<DataContractAttribute>() is null)
            return [];

        return element.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType == element && p.Name != "EqualityContract")
            .SelectMany(p => PartsOf(p.PropertyType))
            .Prepend(element);
    }

    private static Type? ElementType(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ? type.GetGenericArguments()[0] : null;

    private static IEnumerable<Type> ServiceTypes() =>
        ContractsAssembly.GetTypes().Where(t => t.GetCustomAttribute<ServiceContractAttribute>() is not null);

    /// <summary>
    /// The copy serializes to the same bytes. Records with lists do not compare by value, so the bytes are compared; every
    /// property is a data member (see above), so a value lost on the way changes them.
    /// </summary>
    private static void AssertRoundTrip(object message)
    {
        var bytes = Serialize(message);

        var copy = RuntimeTypeModel.Default.Deserialize(new MemoryStream(bytes), null, message.GetType());

        Assert.Equal(bytes, Serialize(copy));
    }

    private static byte[] Serialize(object message)
    {
        using var stream = new MemoryStream();
        RuntimeTypeModel.Default.Serialize(stream, message);

        return stream.ToArray();
    }

    private static object Create(Type type, bool nullableAsNull)
    {
        var constructor = type.GetConstructors().Single(c => c.GetParameters() is not [{ } p] || p.ParameterType != type);
        var context = new NullabilityInfoContext();
        var arguments = constructor.GetParameters()
            .Select(p => nullableAsNull && context.Create(p).WriteState == NullabilityState.Nullable ? null : Sample(p.ParameterType, nullableAsNull))
            .ToArray();

        return constructor.Invoke(arguments);
    }

    private static object Sample(Type type, bool nullableAsNull)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return Enum.GetValues(type).Cast<object>().Last();

        if (ElementType(type) is { } element)
        {
            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            list.Add(Sample(element, nullableAsNull));
            return list;
        }

        if (type.GetCustomAttribute<DataContractAttribute>() is not null)
            return Create(type, nullableAsNull);

        return type switch
        {
            _ when type == typeof(Guid) => Guid.NewGuid(),
            _ when type == typeof(string) => "Text",
            _ when type == typeof(DateOnly) => new DateOnly(2026, 3, 2),
            _ when type == typeof(DateTime) => new DateTime(2026, 3, 2, 9, 30, 0),
            _ when type == typeof(decimal) => 1234.56m,
            _ when type == typeof(int) => 7,
            _ when type == typeof(long) => 42L,
            _ when type == typeof(bool) => true,
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
