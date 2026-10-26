using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectManagementSystem.Application.Projects;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.WorkPackages;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Domain.Interfaces;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Tests.Fakes;
using ProjectManagementSystem.Domain.WorkPackages;
using ProjectManagementSystem.Infrastructure.Identity;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

namespace ProjectManagementSystem.Api.Tests;

/// <summary>
/// The real host over gRPC-Web, as the browser calls it, with the bearer token of <c>/auth/login</c>. The accounts are in
/// an in-memory database, the aggregates in in-memory repositories, and the outbox processor does not run. Only
/// failing commands are sent, apart from one whose aggregate lives in a fake repository, so nothing is written.
/// </summary>
public sealed class ApiHostTests : IClassFixture<ApiHostTests.Factory>
{
    private const string ClientOrigin = "https://localhost:7130";
    private const string AuthPrefix = "/auth";
    private const string Password = "Pa$$w0rd-for-tests";

    private readonly Factory _factory;

    public ApiHostTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Call_without_a_token_is_unauthenticated()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IProjectService>().StartProject(new StartProjectCommand(Guid.NewGuid())));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task Call_with_a_forged_token_is_unauthenticated()
    {
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IProjectService>().StartProject(new StartProjectCommand(Guid.NewGuid()), WithToken("forged")));

        Assert.Equal(StatusCode.Unauthenticated, exception.StatusCode);
    }

    [Fact]
    public async Task Token_of_login_authenticates_the_grpc_call()
    {
        var token = await LoginAsync(Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IProjectService>().StartProject(new StartProjectCommand(Guid.NewGuid()), WithToken(token)));

        // Past authentication: the command ran and did not find the project.
        Assert.Equal(StatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task Token_carries_the_employee_of_the_account_to_the_domain()
    {
        var responsible = Guid.NewGuid();
        var workPackage = SeedWorkPackage(responsible);

        var other = await Assert.ThrowsAsync<RpcException>(async () =>
            await Client<IWorkPackageService>().CompleteWorkPackage(
                new CompleteWorkPackageCommand(workPackage.Id), WithToken(await LoginAsync(Guid.NewGuid()))));

        Assert.Equal(StatusCode.PermissionDenied, other.StatusCode);
        Assert.Equal("Only the responsible person of the work package can complete it.", other.Status.Detail);
        await Client<IWorkPackageService>().CompleteWorkPackage(
            new CompleteWorkPackageCommand(workPackage.Id), WithToken(await LoginAsync(responsible)));
        Assert.Equal(WorkPackageStatus.Completed, workPackage.Status);
    }

    [Fact]
    public async Task Refreshed_token_still_carries_the_employee()
    {
        var employeeId = Guid.NewGuid();
        var login = await LoginResponseAsync(employeeId);
        using var http = _factory.CreateClient();

        using var response = await http.PostAsJsonAsync($"{AuthPrefix}/refresh", new { refreshToken = login.RefreshToken });
        response.EnsureSuccessStatusCode();
        var refreshed = await response.Content.ReadFromJsonAsync<TokenResponse>();

        var workPackage = SeedWorkPackage(responsiblePersonId: Guid.NewGuid());
        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            Client<IWorkPackageService>().CompleteWorkPackage(new CompleteWorkPackageCommand(workPackage.Id), WithToken(refreshed!.AccessToken)));
        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
        Assert.Equal("Only the responsible person of the work package can complete it.", exception.Status.Detail);
    }

    [Fact]
    public async Task Register_is_not_published()
    {
        using var http = _factory.CreateClient();

        using var response = await http.PostAsJsonAsync($"{AuthPrefix}/register", new { email = "new@example.com", password = Password });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(await _factory.Services.CreateScope().ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("new@example.com"));
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

    private async Task<string> LoginAsync(Guid employeeId) => (await LoginResponseAsync(employeeId)).AccessToken;

    /// <summary>
    /// Creates an account of the employee and signs in with it through <c>/auth/login</c>.
    /// </summary>
    private async Task<TokenResponse> LoginResponseAsync(Guid employeeId)
    {
        var email = $"{employeeId}@example.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await users.CreateAsync(new ApplicationUser { UserName = email, Email = email, EmployeeId = employeeId }, Password);
            Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(e => e.Description)));
        }

        using var http = _factory.CreateClient();
        using var response = await http.PostAsJsonAsync($"{AuthPrefix}/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
    }

    private static CallContext WithToken(string token) =>
        new(new CallOptions(new Metadata { { "Authorization", $"Bearer {token}" } }));

    private TService Client<TService>() where TService : class
    {
        var handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, _factory.Server.CreateHandler());
        var channel = GrpcChannel.ForAddress(
            _factory.Server.BaseAddress,
            new GrpcChannelOptions { HttpHandler = handler, HttpVersion = HttpVersion.Version11 });

        return channel.CreateGrpcService<TService>();
    }

    public sealed class Factory : WebApplicationFactory<Program>
    {
        public InMemoryRepository<Project> Projects { get; } = new();

        public InMemoryRepository<WorkPackage> WorkPackages { get; } = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder) =>
            builder.ConfigureTestServices(services =>
            {
                foreach (var options in services.Where(d =>
                             d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                             d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>)).ToList())
                    services.Remove(options);
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));

                services.AddSingleton<IRepository<Project>>(Projects);
                services.AddSingleton<IRepository<WorkPackage>>(WorkPackages);

                foreach (var outbox in services.Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "OutboxProcessor").ToList())
                    services.Remove(outbox);
            });

        private readonly string _databaseName = Guid.NewGuid().ToString();
    }

    private sealed record TokenResponse(string AccessToken, string RefreshToken);
}
