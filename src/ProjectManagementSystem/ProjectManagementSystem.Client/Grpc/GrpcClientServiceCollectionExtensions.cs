using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using ProjectManagementSystem.Application.Dashboard;
using ProjectManagementSystem.Application.Employees;
using ProjectManagementSystem.Application.Funding;
using ProjectManagementSystem.Application.Milestones;
using ProjectManagementSystem.Application.Notifications;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.Risks;
using ProjectManagementSystem.Application.Schedule;
using ProjectManagementSystem.Application.TimeTracking;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Client.Auth;
using ProtoBuf.Grpc.Client;

namespace ProjectManagementSystem.Client.Grpc;

public static class GrpcClientServiceCollectionExtensions
{
    /// <summary>
    /// Registers a typed client for every gRPC service of the API. They are the service interfaces of
    /// Application.Contracts, which the API implements (code-first with protobuf-net.Grpc), so a view injects for
    /// example <see cref="IDashboardService"/> and calls it like a local service. A browser cannot speak HTTP/2 gRPC,
    /// so the calls go as gRPC-Web, which the API enables for every service.
    /// </summary>
    public static IServiceCollection AddGrpcClients(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.AddScoped(provider => GrpcChannel.ForAddress(apiBaseAddress, new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb,
                new BearerTokenHandler(provider.GetRequiredService<AuthService>()) { InnerHandler = new HttpClientHandler() }),
        }));
        services.AddScoped<SessionExpiredInterceptor>();
        services.AddScoped<CallInvoker>(provider => provider.GetRequiredService<GrpcChannel>()
            .Intercept(provider.GetRequiredService<SessionExpiredInterceptor>()));

        services.AddGrpcService<IDashboardService>();
        services.AddGrpcService<IEmployeeService>();
        services.AddGrpcService<IFundingService>();
        services.AddGrpcService<IMilestoneService>();
        services.AddGrpcService<INotificationService>();
        services.AddGrpcService<IProjectService>();
        services.AddGrpcService<IRiskService>();
        services.AddGrpcService<IScheduleService>();
        services.AddGrpcService<ITimeTrackingService>();
        services.AddGrpcService<IWorkPackageService>();

        return services;
    }

    private static void AddGrpcService<TService>(this IServiceCollection services) where TService : class =>
        services.AddScoped(provider => provider.GetRequiredService<CallInvoker>().CreateGrpcService<TService>());
}
