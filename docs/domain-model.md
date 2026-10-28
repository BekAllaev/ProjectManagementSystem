# Domain model

## Projects

| Project | References | Contains |
|---|---|---|
| `Domain.Shared` | — | enums used by the domain and by the contracts (`ProjectState`, `WorkPackageStatus`, `ProjectTaskStatus`, `MilestoneStatus`, `DependencyType`, `RiskLevel`, `RiskStatus`) |
| `Domain` | `Domain.Shared` | aggregates, domain services, specifications, domain events |
| `Application.Contracts` | `Domain.Shared` | `ICommand` / `IQuery`, the command and query records with the read models they return, `CreatedId`, the gRPC service interfaces (one per aggregate, plus notifications and the dashboard): the public surface of the backend |
| `Application` | `Domain`, `Application.Contracts` | command and query handlers with their validators, pipeline behaviors, one event handler per aggregate, the read models, ports for the infrastructure |
| `Infrastructure` | `Application`, `Domain` | persistence (EF Core, SQL Server) with the mapping of the read models, repositories, the unit of work, the outbox and its processor, in-app notifications, the rebuild of the read models (see [Read side](#read-side)) |
| `Api` | `Application`, `Infrastructure` | the gRPC host: one service implementation per service interface (each method only sends the command or query through MediatR), the interceptor that maps exceptions to gRPC status codes (forbidden → PermissionDenied, rule violation → FailedPrecondition, not found → NotFound, domain or FluentValidation validation → InvalidArgument, concurrency conflict → Aborted, cancelled call → Cancelled, anything else → Internal without details), `ICurrentUser` from the `employee_id` claim, CORS for the client; sign-in with ASP.NET Core Identity (`MapIdentityApi` under `/auth`; only `POST /login`, `POST /refresh` and `GET /manage/info` are open, the rest answers 404), whose bearer token authenticates the gRPC calls |

The API exposes every command and query over gRPC (code-first, protobuf-net.Grpc, with gRPC-Web for the browser).
Commands, queries and their results are the gRPC messages themselves: `[DataContract]` with `[DataMember(Order = N)]`
on every property. The orders are part of the wire format and never change; a new property gets the next free
number. protobuf-net needs a parameterless constructor, so every positional record has a private one. A command
without a result answers with an empty message, a command that creates something with `CreatedId`. Every aggregate
has one service interface (`IProjectService`, `IWorkPackageService`, ...) with one method per command; the host
implements a method by sending the command through MediatR, so the pipeline behaviors apply. The contracts never
reference the domain, so no domain type leaks into the API; a .NET client (for example Blazor WebAssembly) shares
them for typed calls. Types keep their namespaces when they move between projects
(for example `DependencyType` stays in `ProjectManagementSystem.Domain.Schedule`). Enum values are explicit,
because they are stored as numbers: a new value never shifts the existing ones.

## Aggregate boundaries

The former "mega aggregate" `Project` is split into several aggregates. Aggregates reference each other
only by id. Every rule that spans more than one aggregate is listed in
[Cross-aggregate rules](#cross-aggregate-rules).

| Aggregate | Root | One per | Folder (`src/ProjectManagementSystem.Domain`) |
|---|---|---|---|
| Project | `Project` | project | `Projects/` |
| WorkPackage | `WorkPackage` | work package | `WorkPackages/` |
| ScheduleNetwork | `ScheduleNetwork` | project | `Schedule/` |
| TaskTimeLog | `TaskTimeLog` | task | `TimeTracking/` |
| Milestone | `Milestone` | milestone | `Milestones/` |
| ProjectFunding | `ProjectFunding` | project | `Funding/` |
| Risk | `Risk` | risk | `Risks/` |
| Employee | `Employee` | employee (shared by all projects) | `Employees/` |

Every aggregate has its events in `Events/`. Domain services are in `Services/`, specifications in
`Specifications/` of the aggregate they are checked on.

Operations whose rule needs another aggregate are `internal` on the aggregate and public on a domain
service, so the rule cannot be bypassed: for example `WorkPackage.AddTask` is called only by
`WorkPackageService.AddTask`, which checks the team of the project first.

## Repositories, domain services and application services

- **One repository** `IRepository<TAggregate>` for all aggregates, with CRUD by id: `FindAsync`, `Add`,
  `Update`, `Delete`, and one search, `ExistsAsync(predicate)`, for rules that depend on a whole set of
  aggregates. The predicate is translated by the storage, so it compares with values computed beforehand
  and does not call domain code. There is deliberately no loading of lists by a predicate (see
  [Considered alternatives](#considered-alternatives)); there are no per-aggregate repository interfaces. It accepts only aggregate roots, so inner entities (tasks, time entries, roles) are
  never loaded on their own.
- An aggregate that exists once per project or per task uses that id as its own id: `ScheduleNetwork.Id` and
  `ProjectFunding.Id` are the project id, `TaskTimeLog.Id` is the task id. So they are found by id too.
- Queries (CQRS) do not use the repositories of the aggregates: they read the read models (see [Read side](#read-side)).
  The caller of a command passes the ids it already knows (for example the work package of a
  task), and the domain service checks that they fit together (the task is really in that work package).
  Where a rule needs *all* work packages of a project (closing the project, leaving the team), the caller
  loads them by `Project.WorkPackageIds`, and the domain service checks that the set is exactly that list.
- **Domain services are named after the aggregate they change** (`ProjectService`, `WorkPackageService`,
  `ScheduleNetworkService`, `TaskTimeLogService`, `MilestoneService`, `FundingService`, `EmployeeService`), so
  a new rule on an aggregate goes into its service instead of a new service per rule.
- **By default a domain service gets loaded aggregates as arguments**, checks the rule and changes one
  aggregate. If a new aggregate is needed (a time log, a milestone, funding), the service creates it and the
  caller adds it to the repository. A domain service uses a repository only when the rule depends on the
  completeness of a set of aggregates that cannot be listed otherwise: the unique email of an employee
  (`EmployeeService` with `IRepository<Employee>.ExistsAsync`).
- **One aggregate per transaction, without exceptions.** Creating a work package only reads `Project`;
  the package is registered in `Project` after the commit by the handler of `WorkPackageCreated`
  (`Project.RegisterWorkPackage`), with a compensation if the project was closed in the meantime
  (see [Project](#1-project)).
- **Command handlers** (CQRS, one handler per command, in `Application/<Aggregate>/Commands/<UseCase>/`; the
  command record itself is in the same folder of `Application.Contracts`)
  load the aggregates, call the domain service or the aggregate method and return. The unit of work is
  saved once by a pipeline behavior. Handlers contain no business rules; there are no application
  services.
- Domain services that need the current date get `System.TimeProvider` through the constructor
  (`WorkPackageService`, `TaskTimeLogService`, `ProjectService`); "today" is
  `DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)`.
- Aggregates never read the clock: methods that record a moment get it as a parameter
  (`new Project(..., creationDate)`, `Project.Start(startDate)`, `Project.ChangeHourlyRate(..., today)`,
  `Milestone.Complete(completedAt)`); the caller takes it from `TimeProvider`.

### 1. Project

**Root:** `Project`: `Id`, `Title`, `Description`, `Currency`, `State` (Created / InProgress / Completed),
`CreationDate`, `StartDate`, `CompletionDate`, `WorkPackageIds`

`IsOpen` is true for Created and InProgress (planning happens before the start) and false for Completed.
A closed project gets no new work packages, milestones, milestone tasks, dependencies or funding. Team, role
and hourly-rate operations do not check it yet (see [Growth points](#growth-points)).

`WorkPackageIds` holds only the ids of all registered work packages of the project, finished ones
included, never references to `WorkPackage`. `WorkPackageService.CreateWorkPackage` creates a package only
in an open project and does not change `Project`. The handler of `WorkPackageCreated` then calls
`RegisterWorkPackage` (eventual consistency); registering the same package again changes nothing. A change
of the list changes the version of `Project` like any other change inside the aggregate (the persistence
mapping must keep it so), so registering a package and closing the project conflict with each other.

**Compensation.** Between the creation of a package and its registration the project can be closed:
`ProjectService.Complete` does not see the unregistered package. The handler of `WorkPackageCreated` then
does not register it but cancels it (reason "project closed"), which cancels its tasks with the usual
events. So a closed project never keeps an open package.

`Currency` is the currency of all money of the project: hourly rates, time entries and funding. It stays on
`Project` because the hourly rates of the team are in it.

**Inside:**
- **Role** (entity): `Id`, `Name`, `Description`
- **TeamMember** (entity): `Id`, `EmployeeId`, `RoleId`, `From`, `To` (an empty `To` means the employee is
  still on the project), `HourlyRates`
- **HourlyRate** (value object): `Amount`, `From`. A rate is valid until `From` of the next one. The history
  of rates is used only to pick the rate for a time entry (see [TaskTimeLog](#4-tasktimelog)).

**Invariants:**
- periods of the same employee do not overlap, `From ≤ To`;
- `RoleId` of a TeamMember refers to a role of this project;
- role name is unique in the project;
- `RenameRole` / `DeleteRole` only if the role has never been assigned (history counts too);
- hourly rates are never changed retroactively and are never removed; a new rate starts after the current one
  and within the membership period;
- a work package is registered only in an open project;
- `Complete` (internal, called by `ProjectService.Complete`) only from InProgress; it sets `CompletionDate`
  (not earlier than `StartDate`) and the project is closed afterwards.

**Events:** `ProjectCreated`, `ProjectStarted`, `ProjectCompleted`; role and team events.

**References outside:** `EmployeeId`, `WorkPackageIds`

### 2. WorkPackage

**Root:** `WorkPackage`: `Id`, `ProjectId`, `Description`, `ResponsiblePersonId`,
`Status` (Open / Completed / Cancelled), `CancellationReason`

Created only by `WorkPackageService.CreateWorkPackage` (the constructor is internal) in an open project;
it is registered in the project after the commit (see [Project](#1-project)).

**Inside:**
- **Task** (entity, `ProjectTask` in code): `Id`, `Name`, `Description`, `ResponsiblePersonId`,
  `Status` (Open / InProgress / Completed / Cancelled), `EstimatedEffort`

**Invariants:**
- `AddTask` only in an Open package;
- `Complete(completedBy)` only if every task is Completed or Cancelled;
- `Cancel(cancelledBy, reason)` cancels every unfinished task, Completed tasks stay Completed;
- task status transitions: `Start` (Open → InProgress), `Complete` (InProgress → Completed),
  `Cancel` (any unfinished → Cancelled).

**Permissions** (`DomainForbiddenException`; the acting employee comes from `ICurrentUser`, never from the command):
- only the responsible person of the package completes or cancels it;
- the status of a task is changed only by the responsible person of the task or of its package;
- the system cancels a package without an acting person only through `WorkPackageService.CancelInClosedProject`
  (compensation of the handler of `WorkPackageCreated`), which allows it only in a closed project.

**Events:** `WorkPackageCompleted`, `WorkPackageCancelled` (with the ids of the tasks it cancelled),
`TaskCancelled`, `ResponsiblePersonChanged` (`TaskId` is empty for the package itself); also
`WorkPackageCreated`, `TaskAdded`, `TaskStarted`, `TaskCompleted`.

Time spent on tasks is not stored here: it lives in [TaskTimeLog](#4-tasktimelog).

### 3. ScheduleNetwork

One per project, created by the `AddDependency` command handler on the first dependency of the project.
`ScheduleNetworkService.AddDependency` gets the project: the network belongs to it and the project is open.

**Root:** `ScheduleNetwork`: `Id` = ProjectId

**Inside:**
- **Dependency** (value object): `PredecessorTaskId`, `PredecessorWorkPackageId`, `SuccessorTaskId`,
  `SuccessorWorkPackageId`, `Type` (FS / SS / FF / SF)

**Invariant:** the network has no cycles. The check runs on `AddDependency` and throws immediately.

**Methods:** `AddDependency`, `RemoveDependency`, `RemoveDependenciesOf(taskIds)`

**Events:** `DependencyAdded`, `DependencyRemoved`, `PredecessorCancelled`

`RemoveDependenciesOf(taskIds)` raises `PredecessorCancelled` for every removed dependency whose predecessor
is among the cancelled tasks and whose successor is not, so the responsible person of the successor can be
notified.

### 4. TaskTimeLog

One per task, created by `TaskTimeLogService.CreateTimeLog` on the first entry.

**Root:** `TaskTimeLog`: `Id` = TaskId, `WorkPackageId`, `ProjectId`

**Inside:**
- **TimeEntry** (entity with local identity): `Id`, `EmployeeId`, `WorkDate`, `Hours`, `HourlyRate`

The hourly rate in an entry is a snapshot, like the price in an order line: it is the rate of the employee
valid on `WorkDate` and it never changes afterwards, even if the rate of the employee changes later.
The local `Id` makes it possible to point at one wrong entry (for example, one of two identical entries
of 2 hours).

**Methods:**
- `LogTime(employeeId, workDate, hours, hourlyRate)` adds an entry;
- `RemoveEntry(entryId, removedBy)` removes an entry. A mistake is fixed by removing the wrong entry and logging
  the right one.

**Permissions** (`DomainForbiddenException`): an employee logs only their own time (the employee of the entry is
the current user) and only on a work date when they are a member of the project team; an entry is removed only
by the employee who logged it.

**Invariants:**
- `Hours > 0`, `HourlyRate > 0`;
- `LogTime` and `RemoveEntry` only while the task is InProgress; after the task is finished the log does
  not change (checked by the domain service, see [Cross-aggregate rules](#cross-aggregate-rules)).

**Events:**
- `TimeLogged(ProjectId, WorkPackageId, TaskId, EntryId, EmployeeId, WorkDate, Hours, HourlyRate)`;
- `TimeEntryRemoved(ProjectId, TaskId, EntryId)`.

The aggregate grows with the number of entries. For one task it is tens of entries, which is fine.

### 5. Milestone

**Root:** `Milestone`: `Id`, `ProjectId`, `Name`, `Description`, `TaskIds`, `TargetDate`,
`Status` (Open / Completed), `CompletedAt`

Created by `MilestoneService.Create` only in an open project (the constructor is internal). Tasks are added
by `MilestoneService.AddTask` (`Milestone.AddTask` is internal): the milestone and the work package belong to
the project, the project is open, the task exists in the work package and is not cancelled (a completed task
is allowed). `RemoveTask`, `ChangeTargetDate`, `Rename`, `Complete`, `Delete` need nothing outside the
milestone and stay public.

**Invariants:** `AddTask`, `RemoveTask`, `ChangeTargetDate` only while Open; after `Complete()` the
milestone is frozen.

**Events:** `MilestoneCompleted`, `MilestoneDeleted`

### 6. ProjectFunding

One per project, created by `FundingService.CreateFunding` on the first funding of the project (the
constructor is internal). Tranches are added by `FundingService.AddFunding` (`ProjectFunding.AddFunding` is
internal): the funding belongs to the project and the project is open. Amounts are in the currency of the
project.

**Root:** `ProjectFunding`: `Id` = ProjectId

**Inside:**
- **FundingTranche** (value object): `Amount`, `Date`, `Reason`

**Invariant:** `Amount > 0`. There is no budget (BAC, cost baseline): the project is planned iteratively
(rolling wave planning), so funding only grows and may be exceeded.

**Event:** `FundingAdded`

### 7. Risk

**Root:** `Risk`: `Id`, `ProjectId`, `Description`, `Probability`, `Impact`, `OwnerId`, `Status`

Risk is a plain register record (CRUD) without invariants, domain behaviour or events. It takes part in
no rule of the rest of the domain. Following Evans, tactical DDD patterns pay off in the core domain,
while supporting subdomains may stay simple CRUD.

Growth points that would make Risk a real part of the domain:

- a risk response (mitigate etc.) creates tasks / a work package — a risk as a source of scope change;
- contingency reserve — a part of funding justified by specific risks;
- a risk occurring (risk → issue) as an event with consequences;
- opportunities (positive risks).

### 8. Employee

Shared by all projects, projects reference it by id.

**Root:** `Employee`: `Id`, `FirstName`, `LastName`, `Email`, `NormalizedEmail`, `PhotoPath`

The email is unique among all employees, compared normalized (`EmailAddress.Normalize`: trimmed, lower-case,
invariant culture). `Email` keeps the email as entered. An employee is created by
`EmployeeService.RegisterAsync` and its email is changed by `EmployeeService.ChangeEmailAsync` (the constructor
and `ChangeEmail` are internal); both reject an email used by another employee. The check is the
precondition; the last line of defence against two concurrent registrations is a unique index on
`NormalizedEmail` in the database, whose violation the infrastructure translates into a `DomainRuleViolationException`.

An employee signs in with an account (`ApplicationUser` of ASP.NET Core Identity, in the infrastructure). The account and
the employee are different records: `ApplicationUser.EmployeeId` is required, unique and a foreign key to `Employee.Id`,
so an employee has at most one account. The domain knows nothing of accounts; the API puts the employee into the
`employee_id` claim of the token, and handlers receive it as `ICurrentUser.EmployeeId`.

## Cross-aggregate rules

| Rule | Aggregates | How it is enforced |
|---|---|---|
| The responsible person of a package/task is a team member (at assignment time) | WorkPackage ↔ Project | `WorkPackageService` + `IsTeamMemberOnSpecification` |
| A responsible person of an open package/task cannot be removed from the team | Project ↔ WorkPackage | `ProjectService.EndTeamMembership` gets all work packages of the project (exactly `WorkPackageIds`) and rejects with `IsResponsibleForOpenWorkSpecification`. Re-check after commit by `ResponsiblePersonCheck` from the event handlers of the project and the work packages (an assignment racing with leaving the team, a package not registered yet, a membership ending in the future): the management of the project is notified, nothing is changed automatically |
| A project is completed only when every work package is Completed or Cancelled | Project ↔ WorkPackage | `ProjectService.Complete` gets exactly the packages of `WorkPackageIds`; packages never reopen and registering a package changes the version of `Project`. A package created but not yet registered is cancelled by the handler of `WorkPackageCreated` when it finds the project closed (compensation) |
| No new work in a closed project | Project → WorkPackage, ScheduleNetwork, Milestone, ProjectFunding | `WorkPackageService.CreateWorkPackage` checks `Project.IsOpen`, a concurrent closing is compensated by the handler of `WorkPackageCreated`; `ScheduleNetworkService`, `MilestoneService`, `FundingService` check `Project.IsOpen`. A concurrent closing is not detected for ScheduleNetwork, Milestone and ProjectFunding: no invariant breaks, at worst a superfluous record appears; accepted without compensation |
| The email of an employee is unique | Employee ↔ all Employees | `EmployeeService` with `IRepository<Employee>.ExistsAsync`; a unique index on `NormalizedEmail` stops concurrent registrations |
| Cancelling a package/task removes its dependencies | WorkPackage → ScheduleNetwork | `WorkPackageEventHandler` on `WorkPackageCancelled` / `TaskCancelled` → `RemoveDependenciesOf` |
| A dependency links only existing tasks of the project that are not cancelled | ScheduleNetwork ↔ WorkPackage | `ScheduleNetworkService` + `ScheduleNetworkEventHandler` on `DependencyAdded` removes it if a task is already cancelled |
| `LogTime` / `RemoveEntry` only on an InProgress task | TaskTimeLog ↔ WorkPackage | `TaskTimeLogService`; a race with `CompleteTask` is accepted |
| `LogTime`: the employee (the current user) is a team member on `WorkDate` (otherwise `DomainForbiddenException`), `WorkDate` is not in the future, the entry gets the hourly rate valid on `WorkDate` | TaskTimeLog ↔ Project | `TaskTimeLogService` reads the rate from `TeamMember` and passes it to `LogTime`; rates are never changed retroactively, so there is no race to compensate |
| FS predecessor is not finished | ScheduleNetwork ↔ WorkPackage | not blocked; a warning on the read side |
| A task added to a milestone exists in a work package of the project and is not cancelled | Milestone ↔ WorkPackage | `MilestoneService.AddTask`; a concurrent cancellation is accepted without compensation, cancelled tasks are ignored on the read side |

Every aggregate whose events get a reaction has **one event handler**, `<Aggregate>EventHandler` in
`src/ProjectManagementSystem.Application/<Aggregate>/EventHandlers`, for all its events: it keeps the read models of the
aggregate (see [Read side](#read-side)) and does what else the event calls for. Every handler is idempotent, because
the outbox may deliver an event more than once, and saves its own change (one aggregate) with the unit of work. A
handler that also changes an aggregate offers `ProjectAsync` for the events concerned: only the change of the read
models, which the rebuild of the read models uses so that it changes no aggregate and notifies nobody.

| Handler | Events | Effect besides the read models |
|---|---|---|
| `ProjectEventHandler` | project, role and team events, `WorkPackageRegistered` | on `TeamMembershipEnded`: `ResponsiblePersonCheck` of all open work of the employee |
| `WorkPackageEventHandler` | work package and task events | on `WorkPackageCreated`: `Project.RegisterWorkPackage`, or cancels the package if the project is already closed (compensation); on `WorkPackageCreated`, `ResponsiblePersonChanged`, `TaskAdded`: `ResponsiblePersonCheck` of the assignment; on `TaskCancelled`, `WorkPackageCancelled`: `ScheduleNetwork.RemoveDependenciesOf` |
| `ScheduleNetworkEventHandler` | `DependencyAdded`, `DependencyRemoved`, `PredecessorCancelled` | on `DependencyAdded`: removes a dependency on a task cancelled concurrently; on `PredecessorCancelled`: notifies the responsible persons of the unfinished successor task and its package |
| `TaskTimeLogEventHandler` | `TimeLogged`, `TimeEntryRemoved` | — |
| `ProjectFundingEventHandler` | `FundingAdded` | — |
| `MilestoneEventHandler` | milestone events | — |

`ResponsiblePersonCheck` notifies the management of the project about a responsible person of open work without an
ongoing team membership.

Notifications go through `INotificationSender`; each has an idempotency key, and a repeated key is not sent
again. A notification has a `Kind` (`ResponsibleOutsideTeam`, `PredecessorCancelled`; `Other` for rows stored before
the kind existed), a short `Title`, the `Message` and optionally the work package and task it links to.

## Infrastructure

- **Optimistic concurrency** inside every aggregate (aggregate version).
- **Eventual consistency** between aggregates through a **Transactional Outbox** with a single sequential
  processor, so the order of events is preserved.
- **EventId** = `Guid.CreateVersion7()`.
- **AggregateVersion** of an event = the version its aggregate is saved with (0 for a new aggregate), set by the
  unit of work like `EventId`; the read side keeps it as the version of a read model.

Restoring state from events (event sourcing) is not implemented yet.

Persistence and the composition root do not exist yet. When they are added:

- register `TimeProvider.System` for the domain services;
- map `Project.WorkPackageIds` so that a change of only this list updates the version of `Project`
  (for example as a primitive collection stored in the row of the project);
- implement `IRepository<TAggregate>.ExistsAsync` (translated to a query), add a unique index on `Employee.NormalizedEmail` and translate its
  violation into a `DomainRuleViolationException`.

## Read side

Every screen of the client reads a read model whose shape is that of the screen: one table per read model in the
schema `read` (`ProjectItems`, `TaskItems`, `DependencyItems`, `MilestoneItems`, ...). The read models are part of the
query side, not of the domain: their rows are classes in `Application/ReadModels`, mapped by the infrastructure, and
`IReadModelRepository<TRow>` (implemented by the infrastructure) lists, finds, adds and removes rows by a predicate.

The **event handlers** of the aggregates (see [Event handlers](#cross-aggregate-rules)) keep the read models up to
date: a handler changes the rows its event is about and then lets `ProjectReadModelRefresher` recompute what a project
takes from other rows (task counters, progress of packages, milestones and the project, statuses and warnings of
dependencies, logged time and cost, funding figures). Rows are never saved by the handler itself: the outbox saves them
together with the mark that the event was processed (or the unit of work, when the handler also saves an aggregate).
Only values are set and a row is added once (by its id, or by the id of the event for a funding tranche), so a repeated
delivery changes nothing.

A **query handler** lives in `Application/<Aggregate>/Queries/<UseCase>/`, next to its validator, like a command
handler; the query and its result are gRPC messages in the same folder of `Application.Contracts`. It reads the rows
and returns them as they are, deciding only what depends on the caller or on the day, which cannot be stored: who is
a member today, which rate is valid today, whether a milestone is late, and what the caller may do. Employees and
risks have no events, so their aggregates are read as their read models (`IReadModelRepository<Employee>`,
`IReadModelRepository<Risk>`). Notifications are a read model too (`NotificationItemView`), written by the
infrastructure behind `INotificationSender`. Query and event handlers are tested on SQLite in memory, with the data
arranged through commands and the events delivered through the outbox.

The read models are **eventually consistent**: a command is visible to the queries once the outbox has delivered its
events, usually right after the commit. Until then a new project is not found, and the client shows "updating".
`ReadModelRebuilder` builds all read models anew from the aggregates (the migrator does it with
`--rebuild-read-models`), for data saved before the read models existed or after an event handler changed.

| Service | Queries |
|---|---|
| `IProjectService` | `GetProjects` (filter by state and title), `GetProject` (header with task counts and progress), `GetTeam` (one item per employee with all periods and rates), `GetRoles` (with `IsAssigned`) |
| `IWorkPackageService` | `GetWorkPackages` (with tasks and `IsRegistered`), `GetTasks` (filter by status, work package, responsible person), `GetTask` (dependencies and time log) |
| `IScheduleService` | `GetDependencies` |
| `IMilestoneService` | `GetMilestones` |
| `IFundingService` | `GetFunding` (tranches, actual cost by member and by work package) |
| `IRiskService` | `GetRisks` (filter by status) |
| `IEmployeeService` | `GetEmployees` (with the projects of today), `GetCurrentEmployee` |
| `IDashboardService` | `GetDashboard`: open tasks of the caller in all projects, the projects they are on today, funding alerts |
| `INotificationService` | `GetNotifications`, and the commands `MarkNotificationRead`, `MarkAllNotificationsRead` |

**Version.** Every event carries the version of its aggregate (`AggregateVersion`), and the event handlers keep it: a
read model of an aggregate carries its `Version` (for a list, every item has the version of its own aggregate; one of
an aggregate that exists once per project, such as the funding or the schedule network, has `Version` 0 before the
aggregate exists). The version grows with every saved change of the aggregate, so the client can tell whether a read
model already shows a change: for example a new work package has `IsRegistered = false` until its registration is
projected.

**What the caller may do.** Read models tell the client which actions the domain will allow, so it does not offer
the others: `CanChangeStatus` (the task is not finished and the caller is responsible for it or for its package),
`CanLogTime` (the task is in progress and the caller is on the team today; time is logged only for oneself),
`CanRemove` of a time entry (the caller logged it and the task is in progress), `IsAssigned` of a role (it can no
longer be renamed or deleted). The domain still checks every command.

**Notifications.** A caller sees the notifications addressed to them and those for the management of the projects
whose team they are on today (`RecipientEmployeeId` empty). The domain has no role for the management yet, so every
member sees those, and one member marking such a notification read marks it read for all. A notification the caller
does not see is not found.

Milestone (`MilestoneHealth`):

- progress — how many of the counted tasks are done (cancelled tasks stay in the set but are not counted);
- lateness (`Late`, `DaysLate`) — `TargetDate` has passed and the status is still Open;
- "closed with loose ends" (`CompletedWithLooseEnds`) — Completed, but contains unfinished tasks;
- an empty milestone (`CompletedEmpty`, no counted tasks) is not considered reached; otherwise a completed milestone
  is `Reached`.

Schedule:

- a task that is not finished and whose FS predecessor is not completed gets a warning (`HasWarning` of a dependency,
  `HasUnfinishedFinishToStartPredecessor` of a task); starting it is allowed.

Risk:

- score = probability × impact, each counted 1 (low) to 3 (high); the level is High from 6, Medium from 3, Low below.

Funding (from `FundingAdded`, `TimeLogged`, `TimeEntryRemoved`):

- total funding = Σ `FundingTranche.Amount`;
- actual cost (AC) = Σ `Hours` × `HourlyRate` over time entries; AC on a report date (EVM) is the same sum
  filtered by `WorkDate`;
- remaining = total funding − AC at any moment; used percent = AC / total funding, empty without funding;
- past costs are never recalculated: every entry keeps its own rate;
- the dashboard warns about a project of the caller from 80% used or when it is overspent.

The time entries of the funding are rows upserted by `EntryId` on `TimeLogged` and deleted on `TimeEntryRemoved`,
and the sums are recomputed from them; a repeated delivery of an event does not double anything.

## Considered alternatives

- **Loading all work packages of a project by a search on `ProjectId`** instead of keeping `WorkPackageIds`
  in `Project`. Rejected: a search sees only committed packages, so a package created concurrently with
  closing the project would be missed. With the id list, registering a package changes `Project`, and
  closing the project and registering a package become a conflict of versions of `Project`.
- **Registering the package in `Project` in the same transaction as its creation.** It kept the closing
  invariant strict, but broke the rule "one aggregate per transaction". Replaced by registration in the
  handler of `WorkPackageCreated` with a compensation (cancelling the package) if the project is already
  closed.

## Growth points

- The re-check after leaving the team only notifies ([issue #3](https://github.com/BekAllaev/ProjectManagementSystem/issues/3)).
  The domain has no role for the management of the project yet, so the recipient is empty; a domain event for
  manual handling (and an automatic reassignment) are possible next steps. A member added with a fixed end
  date is reported too, because the membership will end while the work is open.
- Whether team, role and hourly-rate operations are allowed in a closed project.

## Open questions

- `FundingExceeded` — there should be an event when funding is exceeded (for notifications). Remaining
  funding is computed, so it is not decided in which operation the excess "happens" and who raises the event.
