using ProjectManagementSystem.Api.Authentication;
using ProjectManagementSystem.Api.Interceptors;
using ProjectManagementSystem.Api.Services;
using ProjectManagementSystem.Application;
using ProjectManagementSystem.Application.Abstractions;
using ProjectManagementSystem.Infrastructure;
using ProtoBuf.Grpc.Server;

// Backend of the Blazor client: every command is a gRPC call (code-first, gRPC-Web for the browser).
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCodeFirstGrpc(options => options.Interceptors.Add<DomainExceptionInterceptor>());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, ClaimsCurrentUser>();

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
app.UseAuthorization();

app.MapGrpcService<EmployeeGrpcService>().EnableGrpcWeb();
app.MapGrpcService<FundingGrpcService>().EnableGrpcWeb();
app.MapGrpcService<MilestoneGrpcService>().EnableGrpcWeb();
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
