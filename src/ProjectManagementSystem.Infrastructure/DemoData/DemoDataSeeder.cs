using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementSystem.Application.Employees.Commands.RegisterEmployee;
using ProjectManagementSystem.Application.Funding.Commands.AddFunding;
using ProjectManagementSystem.Application.Milestones.Commands.AddTaskToMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CompleteMilestone;
using ProjectManagementSystem.Application.Milestones.Commands.CreateMilestone;
using ProjectManagementSystem.Application.Projects.Commands.AddTeamMember;
using ProjectManagementSystem.Application.Projects.Commands.ChangeHourlyRate;
using ProjectManagementSystem.Application.Projects.Commands.CloseProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateProject;
using ProjectManagementSystem.Application.Projects.Commands.CreateRole;
using ProjectManagementSystem.Application.Projects.Commands.StartProject;
using ProjectManagementSystem.Application.Risks.Commands.RegisterRisk;
using ProjectManagementSystem.Application.Risks.Commands.UpdateRisk;
using ProjectManagementSystem.Application.Schedule.Commands.AddDependency;
using ProjectManagementSystem.Application.TimeTracking.Commands.LogTime;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CancelWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CompleteWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateTask;
using ProjectManagementSystem.Application.WorkPackages.Commands.CreateWorkPackage;
using ProjectManagementSystem.Application.WorkPackages.Commands.StartTask;
using ProjectManagementSystem.Domain.Employees;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Domain.Schedule;
using ProjectManagementSystem.Infrastructure.Identity;
using ProjectManagementSystem.Infrastructure.Outbox;
using ProjectManagementSystem.Infrastructure.Persistence;

namespace ProjectManagementSystem.Infrastructure.DemoData;

/// <summary>
/// Fills an empty database with demo data: the employees, projects, teams, work packages, tasks, dependencies,
/// time, funding, milestones and risks of the UI prototype. Everything goes through the real commands, on behalf of
/// the employee who may do it, and every event is delivered through the outbox right away, so the rules of the domain
/// hold, the read models are filled by the event handlers, and the versions are real. Dates are relative to today.
/// </summary>
public sealed class DemoDataSeeder(IServiceScopeFactory scopeFactory, IServiceProvider services, TimeProvider timeProvider)
{
    private Action<Guid> _actAs = _ => { };

    private DateOnly Today => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    /// <param name="actAs">Sets the employee the next command runs for (the current user of the commands).</param>
    /// <param name="password">
    /// Password of a sign-in account created for every employee (email = the email of the employee); no accounts
    /// without it. For a local or test database only.
    /// </param>
    /// <returns>False if the database already has employees: nothing is seeded then.</returns>
    public async Task<bool> SeedAsync(Action<Guid> actAs, string? password = null, CancellationToken cancellationToken = default)
    {
        _actAs = actAs;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            if (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Employee>().AnyAsync(cancellationToken))
                return false;
        }

        var people = await SeedEmployeesAsync(cancellationToken);
        if (password is not null)
            await SeedAccountsAsync(people.All, password, cancellationToken);

        await SeedCustomerPortalAsync(people, cancellationToken);
        await SeedWarehouseScannerAsync(people, cancellationToken);
        await SeedErpMigrationAsync(people, cancellationToken);
        await SeedInternalWikiAsync(people, cancellationToken);
        await SeedBillingServiceAsync(people, cancellationToken);

        return true;
    }

    private async Task<People> SeedEmployeesAsync(CancellationToken cancellationToken)
    {
        async Task<Guid> Employee(string first, string last) =>
            (await SendAsync(Guid.Empty, new RegisterEmployeeCommand(first, last, $"{first}.{last}@projectflow.io".ToLowerInvariant()), cancellationToken)).Id;

        return new People(
            Amira: await Employee("Amira", "Haddad"),
            Jonas: await Employee("Jonas", "Weber"),
            Mei: await Employee("Mei", "Tanaka"),
            Daniel: await Employee("Daniel", "Okafor"),
            Sofia: await Employee("Sofia", "Rossi"),
            Lena: await Employee("Lena", "Ortiz"),
            Tomas: await Employee("Tomas", "Silva"),
            Priya: await Employee("Priya", "Nair"));
    }

    private async Task SeedAccountsAsync(IEnumerable<Guid> employeeIds, string password, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var employees = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Employee>()
            .Where(e => employeeIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var employee in employees)
        {
            var result = await users.CreateAsync(
                new ApplicationUser { UserName = employee.Email, Email = employee.Email, EmployeeId = employee.Id }, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Account of {employee.Email} was not created: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    /// <summary>
    /// The project of the prototype: in progress, with every kind of data.
    /// </summary>
    private async Task SeedCustomerPortalAsync(People p, CancellationToken ct)
    {
        var project = await CreateProjectAsync(
            p.Amira, "Customer Portal Relaunch",
            "Replace the 2014 customer portal with a self-service web app: single sign-on, account dashboard and invoice history.",
            "EUR", ct);
        var manager = await RoleAsync(p.Amira, project, "Project Manager", "Plans and reports the project", ct);
        var backend = await RoleAsync(p.Amira, project, "Backend Developer", "APIs and integrations", ct);
        var frontend = await RoleAsync(p.Amira, project, "Frontend Developer", "Web client", ct);
        var qa = await RoleAsync(p.Amira, project, "QA Engineer", "Testing and quality", ct);
        var analyst = await RoleAsync(p.Amira, project, "Business Analyst", "Requirements", ct);
        await RoleAsync(p.Amira, project, "UX Designer", "Not staffed yet", ct);

        await MemberAsync(p.Amira, project, p.Amira, manager, Today.AddDays(-200), 85m, null, ct);
        await MemberAsync(p.Amira, project, p.Jonas, backend, Today.AddDays(-200), 68m, null, ct);
        await MemberAsync(p.Amira, project, p.Mei, frontend, Today.AddDays(-200), 65m, null, ct);
        await MemberAsync(p.Amira, project, p.Daniel, qa, Today.AddDays(-170), 58m, null, ct);
        await MemberAsync(p.Amira, project, p.Sofia, analyst, Today.AddDays(-190), 58m, Today.AddDays(-150), ct);
        await MemberAsync(p.Amira, project, p.Sofia, analyst, Today.AddDays(-120), 60m, null, ct);
        await MemberAsync(p.Amira, project, p.Lena, frontend, Today.AddDays(-200), 55m, Today.AddDays(-30), ct);
        await SendAsync(p.Amira, new ChangeHourlyRateCommand(project, p.Jonas, 72m, Today.AddDays(7)), ct);
        await SendAsync(p.Amira, new StartProjectCommand(project), ct);

        await SendAsync(p.Amira, new AddFundingCommand(project, 60000m, Today.AddDays(-200), "Initial budget approval"), ct);
        await SendAsync(p.Amira, new AddFundingCommand(project, 40000m, Today.AddDays(-90), "Phase 2 release"), ct);
        await SendAsync(p.Amira, new AddFundingCommand(project, 20000m, Today.AddDays(-40), "Scope extension: SSO"), ct);

        var auth = await WorkPackageAsync(p.Amira, project, "Authentication & SSO", p.Jonas, ct);
        var dashboard = await WorkPackageAsync(p.Amira, project, "Account dashboard UI", p.Mei, ct);
        var payments = await WorkPackageAsync(p.Amira, project, "Payment history", p.Sofia, ct);
        var legacy = await WorkPackageAsync(p.Amira, project, "Legacy portal shutdown", p.Amira, ct);
        var push = await WorkPackageAsync(p.Amira, project, "Mobile push notifications", p.Daniel, ct);

        var login = await TaskAsync(p.Jonas, auth, "Implement OIDC login flow", "Authorization-code flow with PKCE against the corporate identity provider.", p.Jonas, 24m, ct);
        var tenant = await TaskAsync(p.Jonas, auth, "Azure AD tenant configuration", "App registration, redirect URIs and group claims for portal roles.", p.Jonas, 8m, ct);
        var tokens = await TaskAsync(p.Jonas, auth, "Session timeout & refresh tokens", "Silent token refresh, a 30-minute idle timeout and a warning before sign-out.", p.Jonas, 16m, ct);
        var errorPages = await TaskAsync(p.Jonas, auth, "SSO error pages", "Friendly pages for consent denied, account locked and IdP unavailable.", p.Mei, 6m, ct);
        var pentest = await TaskAsync(p.Jonas, auth, "Penetration test for auth", "External pentest of login, logout and token handling before go-live.", p.Daniel, 12m, ct);
        var layout = await TaskAsync(p.Mei, dashboard, "Dashboard layout & navigation", "Responsive shell, navigation and empty states for the account area.", p.Mei, 20m, ct);
        var widgets = await TaskAsync(p.Mei, dashboard, "Usage widgets", "Consumption and contract widgets fed by the usage API.", p.Mei, 16m, ct);
        var accessibility = await TaskAsync(p.Mei, dashboard, "Accessibility audit", "WCAG 2.2 AA review of the dashboard with screen reader checks.", p.Daniel, 8m, ct);
        var invoices = await TaskAsync(p.Sofia, payments, "Invoice list API", "Paged invoice endpoint reading from the billing read model.", p.Jonas, 14m, ct);
        var pdf = await TaskAsync(p.Sofia, payments, "Invoice PDF download", "Signed download links for archived invoice PDFs.", p.Sofia, 10m, ct);
        var redirects = await TaskAsync(p.Amira, legacy, "Redirect old portal URLs", "301 redirects for the 40 most used legacy URLs.", p.Amira, 4m, ct);
        var providers = await TaskAsync(p.Daniel, push, "Push provider evaluation", "Compare FCM, APNs direct and a hosted provider.", p.Daniel, 6m, ct);

        // Work done: started, time logged on past days, completed.
        await WorkAsync(p.Jonas, auth, login, ct, (-60, 6m), (-59, 8m), (-58, 8m), (-57, 4m));
        await CompleteAsync(p.Jonas, auth, login, ct);
        await WorkAsync(p.Jonas, auth, tenant, ct, (-56, 4m), (-55, 3m));
        await CompleteAsync(p.Jonas, auth, tenant, ct);
        await WorkAsync(p.Mei, dashboard, layout, ct, (-50, 8m), (-49, 8m), (-48, 6m));
        await CompleteAsync(p.Mei, dashboard, layout, ct);
        await WorkAsync(p.Amira, legacy, redirects, ct, (-130, 4m));
        await CompleteAsync(p.Amira, legacy, redirects, ct);
        await SendAsync(p.Amira, new CompleteWorkPackageCommand(legacy), ct);

        // Work in progress.
        await WorkAsync(p.Jonas, auth, tokens, ct, (-4, 3.5m), (-3, 4m));
        await WorkAsync(p.Mei, dashboard, widgets, ct, (-5, 6m));
        await WorkAsync(p.Jonas, payments, invoices, ct, (-1, 4m));

        await DependencyAsync(p.Jonas, project, auth, login, auth, tokens, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Jonas, project, auth, tokens, auth, pentest, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Mei, project, dashboard, layout, dashboard, widgets, DependencyType.StartToStart, ct);
        await DependencyAsync(p.Mei, project, dashboard, widgets, dashboard, accessibility, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Sofia, project, payments, invoices, payments, pdf, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Jonas, project, auth, tenant, payments, invoices, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Daniel, project, push, providers, auth, pentest, DependencyType.FinishToStart, ct);

        var legacyOff = await MilestoneAsync(p.Amira, project, "Legacy portal off", "Old portal redirects in place and the 2014 servers switched off.", Today.AddDays(-120), ct, (legacy, redirects));
        await SendAsync(p.Amira, new CompleteMilestoneCommand(legacyOff), ct);
        await MilestoneAsync(p.Amira, project, "Payment history GA", "Invoice list and PDF download available to all customers.", Today.AddDays(-10), ct, (payments, invoices), (payments, pdf));
        await MilestoneAsync(p.Amira, project, "SSO live", "All customers sign in through the corporate identity provider.", Today.AddDays(20), ct, (auth, login), (auth, tenant), (auth, tokens), (auth, pentest));
        await MilestoneAsync(p.Amira, project, "Dashboard beta", "Account dashboard opened to the pilot customer group.", Today.AddDays(35), ct, (dashboard, layout), (dashboard, widgets), (dashboard, accessibility), (push, providers));
        await MilestoneAsync(p.Amira, project, "Public go-live", null, Today.AddDays(50), ct);

        // A package dropped from the scope: its unfinished task is cancelled with it, which removes its dependency and
        // notifies the responsible persons of the successor.
        await WorkAsync(p.Daniel, push, providers, ct, (-20, 2m));
        await SendAsync(p.Daniel, new CancelWorkPackageCommand(push, "Moved to the mobile app project"), ct);
        await SendAsync(p.Jonas, new CancelTaskCommand(auth, errorPages), ct);

        await RiskAsync(p.Amira, project, "IdP outage during the SSO cut-over weekend", RiskLevel.High, RiskLevel.High, p.Jonas, RiskStatus.Open, ct);
        await RiskAsync(p.Amira, project, "Backend capacity drops in Q4 (holiday overlap)", RiskLevel.Medium, RiskLevel.High, p.Amira, RiskStatus.Open, ct);
        await RiskAsync(p.Amira, project, "Legacy ERP invoice data has gaps before 2019", RiskLevel.Medium, RiskLevel.Medium, p.Sofia, RiskStatus.Open, ct);
        await RiskAsync(p.Amira, project, "Pentest vendor slot slips past the SSO go-live", RiskLevel.Medium, RiskLevel.Medium, p.Daniel, RiskStatus.Open, ct);
        await RiskAsync(p.Amira, project, "Browser support matrix grows after the pilot", RiskLevel.Low, RiskLevel.Medium, p.Mei, RiskStatus.Open, ct);
        await RiskAsync(p.Amira, project, "Usage API contract changes late in the project", RiskLevel.Low, RiskLevel.High, p.Mei, RiskStatus.Closed, ct);
    }

    /// <summary>
    /// In progress and overspent.
    /// </summary>
    private async Task SeedWarehouseScannerAsync(People p, CancellationToken ct)
    {
        var project = await CreateProjectAsync(p.Jonas, "Warehouse Scanner App", "Android app for inbound goods scanning", "EUR", ct);
        var developer = await RoleAsync(p.Jonas, project, "Mobile Developer", null, ct);
        var qa = await RoleAsync(p.Jonas, project, "QA Engineer", null, ct);
        await MemberAsync(p.Jonas, project, p.Jonas, developer, Today.AddDays(-150), 70m, null, ct);
        await MemberAsync(p.Jonas, project, p.Daniel, qa, Today.AddDays(-150), 58m, null, ct);
        await SendAsync(p.Jonas, new StartProjectCommand(project), ct);
        await SendAsync(p.Jonas, new AddFundingCommand(project, 1500m, Today.AddDays(-150), "Pilot budget"), ct);

        var scanning = await WorkPackageAsync(p.Jonas, project, "Scanning core", p.Jonas, ct);
        var decoder = await TaskAsync(p.Jonas, scanning, "Barcode decoder benchmarks", "Compare ZXing, ML Kit and a commercial SDK on warehouse labels.", p.Jonas, 12m, ct);
        var offline = await TaskAsync(p.Jonas, scanning, "Offline sync conflict rules", "What wins when two scanners booked the same pallet offline.", p.Jonas, 8m, ct);
        var fieldTest = await TaskAsync(p.Jonas, scanning, "Field test in hall 3", null, p.Daniel, 16m, ct);
        await WorkAsync(p.Jonas, scanning, decoder, ct, (-10, 8m), (-9, 8m), (-8, 6m));
        await DependencyAsync(p.Jonas, project, scanning, decoder, scanning, fieldTest, DependencyType.FinishToStart, ct);
        await DependencyAsync(p.Jonas, project, scanning, offline, scanning, fieldTest, DependencyType.FinishToStart, ct);
    }

    /// <summary>
    /// Created and planned, not started.
    /// </summary>
    private async Task SeedErpMigrationAsync(People p, CancellationToken ct)
    {
        var project = await CreateProjectAsync(p.Priya, "ERP Data Migration", "Move master data out of the legacy ERP", "EUR", ct);
        var lead = await RoleAsync(p.Priya, project, "Data Engineer", null, ct);
        await MemberAsync(p.Priya, project, p.Priya, lead, Today, 75m, null, ct);
        await MemberAsync(p.Priya, project, p.Sofia, lead, Today, 60m, null, ct);
        await SendAsync(p.Priya, new AddFundingCommand(project, 45000m, Today, "Approved budget"), ct);

        var mapping = await WorkPackageAsync(p.Priya, project, "Field mapping", p.Priya, ct);
        await TaskAsync(p.Priya, mapping, "Customer master mapping", null, p.Priya, 24m, ct);
        await TaskAsync(p.Priya, mapping, "Article master mapping", null, p.Sofia, 16m, ct);
        await MilestoneAsync(p.Priya, project, "Mapping signed off", null, Today.AddDays(45), ct);
    }

    /// <summary>
    /// Done and closed.
    /// </summary>
    private async Task SeedInternalWikiAsync(People p, CancellationToken ct)
    {
        var project = await CreateProjectAsync(p.Tomas, "Internal Wiki", "Replace the shared drive with a searchable wiki", "EUR", ct);
        var editor = await RoleAsync(p.Tomas, project, "Editor", null, ct);
        await MemberAsync(p.Tomas, project, p.Tomas, editor, Today.AddDays(-90), 50m, null, ct);
        await SendAsync(p.Tomas, new StartProjectCommand(project), ct);
        await SendAsync(p.Tomas, new AddFundingCommand(project, 4000m, Today.AddDays(-90), "Tooling budget"), ct);

        var setup = await WorkPackageAsync(p.Tomas, project, "Wiki setup", p.Tomas, ct);
        var install = await TaskAsync(p.Tomas, setup, "Install the wiki", null, p.Tomas, 8m, ct);
        var import = await TaskAsync(p.Tomas, setup, "Import the shared drive", null, p.Tomas, 16m, ct);
        await WorkAsync(p.Tomas, setup, install, ct, (-80, 7m));
        await CompleteAsync(p.Tomas, setup, install, ct);
        await WorkAsync(p.Tomas, setup, import, ct, (-70, 8m), (-69, 8m));
        await CompleteAsync(p.Tomas, setup, import, ct);
        await SendAsync(p.Tomas, new CompleteWorkPackageCommand(setup), ct);
        await SendAsync(p.Tomas, new CloseProjectCommand(project), ct);
    }

    /// <summary>
    /// In progress, nearly done, with the caller of the demo accounts on another team.
    /// </summary>
    private async Task SeedBillingServiceAsync(People p, CancellationToken ct)
    {
        var project = await CreateProjectAsync(p.Priya, "Billing Service v2", "Event-driven billing with usage-based pricing", "EUR", ct);
        var developer = await RoleAsync(p.Priya, project, "Backend Developer", null, ct);
        await MemberAsync(p.Priya, project, p.Priya, developer, Today.AddDays(-160), 75m, null, ct);
        await MemberAsync(p.Priya, project, p.Tomas, developer, Today.AddDays(-160), 62m, null, ct);
        await MemberAsync(p.Priya, project, p.Jonas, developer, Today.AddDays(-60), 68m, null, ct);
        await SendAsync(p.Priya, new StartProjectCommand(project), ct);
        await SendAsync(p.Priya, new AddFundingCommand(project, 30000m, Today.AddDays(-160), "Budget"), ct);

        var ingestion = await WorkPackageAsync(p.Priya, project, "Usage ingestion", p.Priya, ct);
        var tasks = new List<Guid>();
        foreach (var name in new[] { "Event schema", "Kafka consumer", "Idempotent writer", "Backfill job" })
        {
            var task = await TaskAsync(p.Priya, ingestion, name, null, p.Tomas, 12m, ct);
            await WorkAsync(p.Tomas, ingestion, task, ct, (-40 + tasks.Count, 6m));
            await CompleteAsync(p.Tomas, ingestion, task, ct);
            tasks.Add(task);
        }

        var reconciliation = await TaskAsync(p.Priya, ingestion, "Reconciliation report", null, p.Jonas, 10m, ct);
        await WorkAsync(p.Jonas, ingestion, reconciliation, ct, (-2, 3m));
        await RiskAsync(p.Priya, project, "Usage events arrive out of order", RiskLevel.Medium, RiskLevel.High, p.Tomas, RiskStatus.Open, ct);
    }

    private async Task<Guid> CreateProjectAsync(Guid by, string title, string description, string currency, CancellationToken ct) =>
        (await SendAsync(by, new CreateProjectCommand(title, description, currency), ct)).Id;

    private async Task<Guid> RoleAsync(Guid by, Guid project, string name, string? description, CancellationToken ct) =>
        (await SendAsync(by, new CreateRoleCommand(project, name, description), ct)).Id;

    private Task MemberAsync(Guid by, Guid project, Guid employee, Guid role, DateOnly from, decimal rate, DateOnly? to, CancellationToken ct) =>
        SendAsync(by, new AddTeamMemberCommand(project, employee, role, from, rate, to), ct);

    private async Task<Guid> WorkPackageAsync(Guid by, Guid project, string description, Guid responsible, CancellationToken ct) =>
        (await SendAsync(by, new CreateWorkPackageCommand(project, description, responsible), ct)).Id;

    private async Task<Guid> TaskAsync(
        Guid by, Guid workPackage, string name, string? description, Guid responsible, decimal effort, CancellationToken ct) =>
        (await SendAsync(by, new CreateTaskCommand(workPackage, name, description, responsible, effort), ct)).Id;

    /// <summary>
    /// Starts the task and logs time of <paramref name="by"/> on the given days relative to today.
    /// </summary>
    private async Task WorkAsync(Guid by, Guid workPackage, Guid task, CancellationToken ct, params (int Day, decimal Hours)[] entries)
    {
        await SendAsync(by, new StartTaskCommand(workPackage, task), ct);
        foreach (var (day, hours) in entries)
            await SendAsync(by, new LogTimeCommand(workPackage, task, Today.AddDays(day), hours), ct);
    }

    private Task CompleteAsync(Guid by, Guid workPackage, Guid task, CancellationToken ct) =>
        SendAsync(by, new CompleteTaskCommand(workPackage, task), ct);

    private Task DependencyAsync(
        Guid by, Guid project, Guid predecessorPackage, Guid predecessor, Guid successorPackage, Guid successor, DependencyType type,
        CancellationToken ct) =>
        SendAsync(by, new AddDependencyCommand(project, predecessorPackage, predecessor, successorPackage, successor, type), ct);

    private async Task<Guid> MilestoneAsync(
        Guid by, Guid project, string name, string? description, DateOnly target, CancellationToken ct, params (Guid WorkPackage, Guid Task)[] tasks)
    {
        var milestone = (await SendAsync(by, new CreateMilestoneCommand(project, name, description, target), ct)).Id;
        foreach (var (workPackage, task) in tasks)
            await SendAsync(by, new AddTaskToMilestoneCommand(milestone, workPackage, task), ct);

        return milestone;
    }

    private async Task RiskAsync(
        Guid by, Guid project, string description, RiskLevel probability, RiskLevel impact, Guid owner, RiskStatus status, CancellationToken ct)
    {
        var risk = (await SendAsync(by, new RegisterRiskCommand(project, description, probability, impact, owner), ct)).Id;
        if (status != RiskStatus.Open)
            await SendAsync(by, new UpdateRiskCommand(risk, description, probability, impact, owner, status), ct);
    }

    /// <summary>
    /// Sends the command on behalf of <paramref name="by"/> in its own scope, as a request, then delivers the events it
    /// saved, as the outbox processor would.
    /// </summary>
    private async Task<TResponse> SendAsync<TResponse>(Guid by, IRequest<TResponse> command, CancellationToken cancellationToken)
    {
        _actAs(by);

        TResponse response;
        await using (var scope = scopeFactory.CreateAsyncScope())
            response = await scope.ServiceProvider.GetRequiredService<ISender>().Send(command, cancellationToken);

        await services.GetRequiredService<IOutboxDrainer>().DrainAsync(cancellationToken);
        await EnsureEveryEventWasDeliveredAsync(cancellationToken);

        return response;
    }

    /// <summary>
    /// The drain records a failed delivery and goes on; for demo data a failure is a bug, so it stops the seeding.
    /// </summary>
    private async Task EnsureEveryEventWasDeliveredAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var failed = await scope.ServiceProvider.GetRequiredService<AppDbContext>().OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .Select(m => m.EventType + ": " + m.LastError)
            .FirstOrDefaultAsync(cancellationToken);

        if (failed is not null)
            throw new InvalidOperationException($"An event of the demo data was not delivered. {failed}");
    }

    private sealed record People(Guid Amira, Guid Jonas, Guid Mei, Guid Daniel, Guid Sofia, Guid Lena, Guid Tomas, Guid Priya)
    {
        public IEnumerable<Guid> All => [Amira, Jonas, Mei, Daniel, Sofia, Lena, Tomas, Priya];
    }
}
