using MudBlazor.Services;
using ProjectManagementSystem.Components;

// Host of the Blazor client: serves the WebAssembly app, which runs in the browser and calls ProjectManagementSystem.Api
// itself (gRPC-Web, bearer token). This host has no data and no sign-in of its own.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// The router of the client checks [Authorize] of the pages, also when the error page renders statically here, so
// the host needs the services of authorization, though it has no sign-in of its own (see MapRazorComponents below).
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(ProjectManagementSystem.Client._Imports).Assembly)
    // Every page is served to everyone: the client decides in the browser whether the user is signed in (and sends
    // them to the login page if not), and the API checks the token of every call. Without this, the [Authorize] of
    // the pages would make this host challenge a sign-in it does not have.
    .AllowAnonymous();

app.Run();
