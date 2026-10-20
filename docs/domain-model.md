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
- **HourlyRate** (value object): `Amount`, `From`. A rate is valid until `From` of the next one. The history
  of rates is used only to pick the rate for a time entry (see [TaskTimeLog](#4-tasktimelog)).

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

**Events:** `DependencyAdded`, `DependencyRemoved`, `PredecessorCancelled`

`RemoveDependenciesOf(taskIds)` raises `PredecessorCancelled` for every removed dependency whose predecessor
is among the cancelled tasks and whose successor is not, so the responsible person of the successor can be
notified.

### 4. TaskTimeLog

One per task, created on the first `LogTime`.

**Root:** `TaskTimeLog`: `Id` = TaskId, `WorkPackageId`, `ProjectId`

**Inside:**
- **TimeEntry** (entity with local identity): `Id`, `EmployeeId`, `WorkDate`, `Hours`, `HourlyRate`

The hourly rate in an entry is a snapshot, like the price in an order line: it is the rate of the employee
valid on `WorkDate` and it never changes afterwards, even if the rate of the employee changes later.
The local `Id` makes it possible to point at one wrong entry (for example, one of two identical entries
of 2 hours).

**Methods:**
- `LogTime(employeeId, workDate, hours, hourlyRate)` adds an entry;
- `RemoveEntry(entryId)` removes an entry. A mistake is fixed by removing the wrong entry and logging
  the right one.

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
| `LogTime` / `RemoveEntry` only on an InProgress task | TaskTimeLog ↔ WorkPackage | domain service; a race with `CompleteTask` is accepted |
| `LogTime`: the employee is a team member on `WorkDate`, `WorkDate` is not in the future, the entry gets the hourly rate valid on `WorkDate` | TaskTimeLog ↔ Project | domain service reads the rate from `TeamMember` and passes it to `LogTime`; rates are never changed retroactively, so there is no race to compensate |
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

Funding (from `FundingAdded`, `TimeLogged`, `TimeEntryRemoved`):

- total funding = Σ `FundingTranche.Amount`;
- actual cost (AC) = Σ `Hours` × `HourlyRate` over time entries; AC on a report date (EVM) is the same sum
  filtered by `WorkDate`;
- remaining = total funding − AC at any moment;
- past costs are never recalculated: every entry keeps its own rate.

The read model upserts a row by `EntryId` on `TimeLogged` and deletes it on `TimeEntryRemoved`, then
recomputes the sums. A repeated delivery of an event does not double anything, so no separate
deduplication is needed.

## Open questions

- `FundingExceeded` — there should be an event when funding is exceeded (for notifications). Remaining
  funding is computed, so it is not decided in which operation the excess "happens" and who raises the event.
