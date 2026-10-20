# Domain model

## Aggregate boundaries

The former "mega aggregate" `Project` is split into several aggregates. Aggregates reference each other
only by id. Every rule that spans more than one aggregate is listed in
[Cross-aggregate rules](#cross-aggregate-rules).

| Aggregate | Root | One per |
|---|---|---|
| Project | `Project` | project |
| WorkPackage | `WorkPackage` | work package |
| ScheduleNetwork | `ScheduleNetwork` | project |
| TaskTimeLog | `TaskTimeLog` | task |
| Milestone | `Milestone` | milestone |
| ProjectFunding | `ProjectFunding` | project |
| Risk | `Risk` | risk |
| Employee | `Employee` | employee (shared by all projects) |

### 1. Project

**Root:** `Project`: `Id`, `Title`, `Description`, `State` (Created / InProgress / Completed),
`CreationDate`, `StartDate`, `CompletionDate`

**Inside:**
- **Role** (entity): `Id`, `Name`, `Description`
- **TeamMember** (entity): `Id`, `EmployeeId`, `RoleId`, `From`, `To` (an empty `To` means the employee is
  still on the project), `HourlyRates`
- **HourlyRate** (value object): `Amount`, `From`. A rate is valid until `From` of the next one.

**Invariants:**
- periods of the same employee do not overlap, `From ≤ To`;
- `RoleId` of a TeamMember refers to a role of this project;
- role name is unique in the project;
- `RenameRole` / `DeleteRole` only if the role has never been assigned (history counts too);
- hourly rates are never changed retroactively and are never removed; a new rate starts after the current one
  and within the membership period.

**References outside:** `EmployeeId`

### 2. WorkPackage

**Root:** `WorkPackage`: `Id`, `ProjectId`, `Description`, `ResponsiblePersonId`,
`Status` (Open / Completed / Cancelled), `CancellationReason`

**Inside:**
- **Task** (entity, `ProjectTask` in code): `Id`, `Name`, `Description`, `ResponsiblePersonId`,
  `Status` (Open / InProgress / Completed / Cancelled), `EstimatedEffort`

**Invariants:**
- `AddTask` only in an Open package;
- `Complete()` only if every task is Completed or Cancelled;
- `Cancel()` cancels every unfinished task, Completed tasks stay Completed;
- task status transitions: `Start` (Open → InProgress), `Complete` (InProgress → Completed),
  `Cancel` (any unfinished → Cancelled).

**Events:** `WorkPackageCompleted`, `WorkPackageCancelled`, `TaskCancelled`, `ResponsiblePersonChanged`

Time spent on tasks is not stored here: it lives in [TaskTimeLog](#4-tasktimelog).

### 3. ScheduleNetwork

One per project.

**Root:** `ScheduleNetwork`: `Id`, `ProjectId`

**Inside:**
- **Dependency** (value object): `PredecessorTaskId`, `PredecessorWorkPackageId`, `SuccessorTaskId`,
  `SuccessorWorkPackageId`, `Type` (FS / SS / FF / SF)

**Invariant:** the network has no cycles. The check runs on `AddDependency` and throws immediately.

**Methods:** `AddDependency`, `RemoveDependency`, `RemoveDependenciesOf(taskIds)`

**Events:** `DependencyAdded`, `DependencyRemoved`

### 4. TaskTimeLog

One per task, created on the first `SetHours`.

**Root:** `TaskTimeLog`: `Id` = TaskId, `WorkPackageId`

**Inside:**
- **EmployeeTime** (value object): `EmployeeId`, `Hours`. One record per employee.

**Methods:** `SetHours(employeeId, hours)` overwrites the value. History is not stored in the aggregate.

**Event:** `WorkedHoursSet` with the absolute number of hours.

### 5. Milestone

**Root:** `Milestone`: `Id`, `ProjectId`, `Name`, `Description`, `TaskIds`, `TargetDate`,
`Status` (Open / Completed), `CompletedAt`

**Invariants:** `AddTask`, `RemoveTask`, `ChangeTargetDate` only while Open; after `Complete()` the
milestone is frozen.

**Events:** `MilestoneCompleted`, `MilestoneDeleted`

### 6. ProjectFunding

One per project.

**Root:** `ProjectFunding`: `Id`, `ProjectId`, `Currency`

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

**Root:** `Employee`: `Id`, `FirstName`, `LastName`, `Email`, `PhotoPath`

## Cross-aggregate rules

| Rule | Aggregates | How it is enforced |
|---|---|---|
| The responsible person of a package/task is a team member (at assignment time) | WorkPackage ↔ Project | domain service + Specification |
| A responsible person of an open package/task cannot be removed from the team | Project ↔ WorkPackage | domain service rejects; re-check after commit with the same Specification, a violation raises `ResponsiblePersonLeftTeam` for manual handling |
| Cancelling a package/task removes its dependencies | WorkPackage → ScheduleNetwork | handler of `WorkPackageCancelled` / `TaskCancelled` → `RemoveDependenciesOf` |
| A dependency links only tasks that are not cancelled | ScheduleNetwork ↔ WorkPackage | domain service + handler of `DependencyAdded` removes it if a task is already cancelled |
| `SetHours` only on an InProgress task, by a current team member | TaskTimeLog ↔ WorkPackage, Project | domain service; a race with `CompleteTask` is accepted |
| FS predecessor is not finished | ScheduleNetwork ↔ WorkPackage | not blocked; a warning on the read side |
| Tasks in a milestone | Milestone ↔ WorkPackage | only references by TaskId; cancelled tasks are ignored on the read side |

## Infrastructure

- **Optimistic concurrency** inside every aggregate (aggregate version).
- **Eventual consistency** between aggregates through a **Transactional Outbox** with a single sequential
  processor, so the order of events is preserved.
- **EventId** = `Guid.CreateVersion7()`.

Restoring state from events (event sourcing) is not implemented yet.

## Read side (computed, not stored)

Milestone:

- progress — how many of the counted tasks are done (cancelled tasks stay in the set but are not counted);
- lateness — `TargetDate` has passed and the status is still Open;
- "closed with loose ends" — Completed, but contains unfinished tasks;
- an empty milestone is not considered reached.

Schedule:

- a task whose FS predecessor is not finished gets a warning.

Funding (from `FundingAdded`, `WorkedHoursSet`, `TeamMemberAdded`, `HourlyRateChanged`):

- total funding = Σ `FundingTranche.Amount`;
- actual cost (AC): every `WorkedHoursSet` adds (new hours − previous hours) × the employee's hourly rate
  valid on the date of the event;
- remaining = total funding − AC at any moment;
- past costs are not recalculated: a rate cannot be changed retroactively.

## Open questions

- `FundingExceeded` — there should be an event when funding is exceeded (for notifications). Remaining
  funding is computed, so it is not decided in which operation the excess "happens" and who raises the event.
