using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectManagementSystem.Api.Authentication;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

namespace ProjectManagementSystem.Api.Tests;

/// <summary>
/// The real host over gRPC-Web, as the browser calls it. Authentication is not configured by the host yet, so the
/// tests add a scheme that trusts a header; repositories are in memory and the outbox processor does not run.
/// Only failing calls are made, so nothing reaches the database.
/// </summary>
public sealed class ApiHostTests : IClassFixture<ApiHostTests.Factory>
{
    private const string ClientOrigin = "https://localhost:7130";

    private readonly Factory _factory;

    public ApiHostTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Unauthenticated_call_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IProjectService>().StartProject(new StartProjectCommand(Guid.NewGuid())));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task Missing_aggregate_comes_back_as_not_found()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IProjectService>().StartProject(new StartProjectCommand(Guid.NewGuid()), As(Guid.NewGuid().ToString())));

        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Rule_of_permission_comes_back_as_permission_denied()
    {
        var workPackage = SeedWorkPackage(responsiblePersonId: Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IWorkPackageService>().CompleteWorkPackage(new CompleteWorkPackageCommand(workPackage.Id), As(Guid.NewGuid().ToString())));

        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
    }

    [Fact]
    public async Task Account_without_an_employee_is_forbidden_when_the_command_needs_the_caller()
    {
        var workPackage = SeedWorkPackage(responsiblePersonId: Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IWorkPackageService>().CompleteWorkPackage(new CompleteWorkPackageCommand(workPackage.Id), As(TestAuthenticationHandler.AccountOnly)));

        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
        Assert.Equal("The caller is not linked to an employee.", exception.Status.Detail);
    }

    [Fact]
    public async Task Response_to_the_client_origin_exposes_the_grpc_headers()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/ProjectManagementSystem.ProjectService/StartProject")
        {
            Content = new ByteArrayContent([0, 0, 0, 0, 0]),
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc-web");
        request.Headers.Add("Origin", ClientOrigin);

        using var response = await client.SendAsync(request);

        Assert.Equal(ClientOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var exposed = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"));
        Assert.All(["Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"], header => Assert.Contains(header, exposed));
    }

    [Fact]
    public async Task Preflight_of_another_origin_is_not_allowed()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/ProjectManagementSystem.ProjectService/StartProject");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private WorkPackage SeedWorkPackage(Guid responsiblePersonId)
    {
        var project = TestData.ProjectWithTeam(responsiblePersonId);
        var workPackage = TestData.WorkPackage(project, responsiblePersonId);
        _factory.Projects.Add(project);
        _factory.WorkPackages.Add(workPackage);

        return workPackage;
    }

    private static CallContext As(string caller) =>
        new(new CallOptions(new Metadata { { TestAuthenticationHandler.Header, caller } }));

    private TService Client<TService>() where TService : class
    {
        var handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, _factory.Server.CreateHandler()) { HttpVersion = new Version(1, 1) };
        var channel = GrpcChannel.ForAddress(_factory.Server.BaseAddress, new GrpcChannelOptions { HttpHandler = handler });

        return channel.CreateGrpcService<TService>();
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public InMemoryRepository<Project> Projects { get; } = new();

        public InMemoryRepository<WorkPackage> WorkPackages { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestAuthenticationHandler.Scheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, null);

                services.AddSingleton<IRepository<Project>>(Projects);
                services.AddSingleton<IRepository<WorkPackage>>(WorkPackages);

                foreach (var outbox in services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "OutboxProcessor").ToList())
                    services.Remove(outbox);
            });
    }

    /// <summary>
    /// Authenticates a call that carries <see cref="Header"/>: a Guid becomes the employee of the caller,
    /// <see cref="AccountOnly"/> an account without an employee.
    /// </summary>
    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Scheme = "Test";
        public const string Header = "x-test-caller";
        public const string AccountOnly = "account-only";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Header, out var caller))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };
            if (caller != AccountOnly)
                claims.Add(new Claim(EmployeeClaimTypes.EmployeeId, caller.ToString()));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));
        }
    }
}
