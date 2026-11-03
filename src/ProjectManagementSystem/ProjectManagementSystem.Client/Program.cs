using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ProjectManagementSystem.Client.Auth;
using ProjectManagementSystem.Client.Grpc;
using ProjectManagementSystem.Client.State;
using ProjectManagementSystem.Client.Theme;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<NotificationEvents>();

var apiBaseAddress = new Uri(builder.Configuration["Api:BaseAddress"]
    ?? throw new InvalidOperationException("Api:BaseAddress is not configured in wwwroot/appsettings.json."));

// Sign-in: /auth/login and /auth/refresh of the API issue the bearer token, kept in the storage of the browser.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<TokenStorage>();
builder.Services.AddScoped(_ => new AuthApiClient(new HttpClient { BaseAddress = apiBaseAddress }));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider>(services => services.GetRequiredService<AuthService>());

// Every command and query: gRPC-Web to the API, through the service interfaces of Application.Contracts.
builder.Services.AddGrpcClients(apiBaseAddress);

await builder.Build().RunAsync();
