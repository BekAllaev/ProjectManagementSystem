using ProjectManagementSystem.Api.Authentication;
using ProjectManagementSystem.Api.Interceptors;
using ProjectManagementSystem.Api.Services;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Infrastructure;
using ProjectManagementSystem.Infrastructure.Identity;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProtoBuf.Grpc.Server;

// Backend of the Blazor client: every command and query is a gRPC call (code-first, gRPC-Web for the browser).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCodeFirstGrpc(options => options.Interceptors.Add<DomainExceptionInterceptor>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();

// Login and refresh go over HTTP (/auth/login, /auth/refresh); the bearer token they issue authenticates the gRPC
// calls. AddIdentityApiEndpoints makes the bearer-or-cookie scheme of Identity the default one.
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<EmployeeClaimsPrincipalFactory>();
builder.Services.AddAuthorization();

// The client runs on another origin. A browser lets it read the gRPC status only if these headers are exposed.
var clientOrigins = builder.Configuration.GetSection("Cors:ClientOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(clientOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));

var app = builder.Build();

app.UseGrpcWeb();
app.UseCors();
app.UseAuthEndpointsAllowList();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();

app.MapGrpcService<DashboardGrpcService>().EnableGrpcWeb();
app.MapGrpcService<EmployeeGrpcService>().EnableGrpcWeb();
app.MapGrpcService<FundingGrpcService>().EnableGrpcWeb();
app.MapGrpcService<MilestoneGrpcService>().EnableGrpcWeb();
app.MapGrpcService<NotificationGrpcService>().EnableGrpcWeb();
app.MapGrpcService<ProjectGrpcService>().EnableGrpcWeb();
app.MapGrpcService<RiskGrpcService>().EnableGrpcWeb();
app.MapGrpcService<ScheduleGrpcService>().EnableGrpcWeb();
app.MapGrpcService<TimeTrackingGrpcService>().EnableGrpcWeb();
app.MapGrpcService<WorkPackageGrpcService>().EnableGrpcWeb();

app.Run();

/// <summary>
/// Entry point; public for the host tests.
/// </summary>
public partial class Program;
