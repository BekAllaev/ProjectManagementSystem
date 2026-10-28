# План реализации Infrastructure

Фазовый план для слоя Infrastructure прототипа ProjectManagementSystem (DDD + CQRS). После каждой фазы — стоп на ревью: сборка зелёная, все тесты проходят, изменения закоммичены (Conventional Commits), дальше — только после подтверждения.

**Интеграционные тесты и база данных — не запускать.** Claude Code не запускает интеграционные тесты, не применяет миграции к базе и не подключается к SQL Server. Это делает автор локально. Claude Code запускает только `Domain.Tests` и `Application.Tests` и проверяет, что проект интеграционных тестов собирается (`dotnet build`). В отчёте по фазе явно указать, что интеграционные тесты не запускались и что нужно прогнать локально.

## Принятые решения (не пересматривать в ходе реализации)

- **СУБД:** SQL Server. Локально — установленный SQL Server (без Docker). В облаке — Azure SQL Database free tier с auto-pause при исчерпании лимита. Провайдер `Microsoft.EntityFrameworkCore.SqlServer`, `EnableRetryOnFailure()` (serverless-база просыпается с задержкой).
- **Маппинг:** EF Core, private parameterless конструкторы «для EF» во всех агрегатах и сущностях (существующие конструкторы поднимают доменные события и не должны вызываться при материализации). Внутренние коллекции сущностей и value objects — `OwnsMany`.
- **Optimistic concurrency:** `Version` (long) — concurrency token. Увеличивает UoW у каждого агрегата, переданного в `repository.Update(...)`, т. к. изменение только owned-коллекции не помечает корень Modified.
- **Время:** `TimeProvider` везде, включая доменные события и тесты. Системные часы (`DateTime.UtcNow`, `Guid.CreateVersion7()` без аргумента) в production-коде и тестах не используются.
- **Transactional Outbox:** события пишутся в outbox в том же `SaveChanges`, что и агрегат. Processor — `BackgroundService` без polling: будится сигналом после commit + один проход при старте.
- **Порядок outbox:** строгий глобальный порядок по `Sequence`. Упавшее сообщение — очередь ждёт его повтора (backoff). После N неудачных попыток тип события паркуется (ключ парковки = тип события): это сообщение и все последующие сообщения того же типа получают статус Parked и не обрабатываются, остальная очередь идёт дальше. Парковка ≠ удаление: сообщения остаются для ручного разблокирования.
- **Read-модели:** та же база, схема `read`. В этом плане не реализуются (будут вместе с фронтом). Сделано позже: таблицы read-моделей в схеме `read`, их обновляют проекции через outbox (см. «Read side» в `domain-model.md`).
- **Уведомления:** in-app таблица `Notifications`, unique index на `IdempotencyKey`.
- **Уникальный email:** unique index на `Employees.NormalizedEmail` как страховка от гонки.
- **Интеграционные тесты:** только локально против установленного SQL Server, не в CI, без контейнеров.

## Точки роста (не реализовывать, только для текста)

Брокер сообщений (Azure Service Bus); per-aggregate ordering вместо глобального; состояние доставки per-handler (ключ парковки «тип события + handler»); параллельная обработка outbox / несколько инстансов; запуск интеграционных тестов в CI; Event Sourcing.

## Фаза 0 — Подготовка Domain и Application

1. Private parameterless конструкторы во всех агрегатах и сущностях: `Project`, `Role`, `TeamMember`, `WorkPackage`, `ProjectTask`, `ScheduleNetwork`, `Milestone`, `ProjectFunding`, `TaskTimeLog`, `TimeEntry`, `Risk`, `Employee`. Для value objects-record'ов, маппящихся через `OwnsMany` (`HourlyRate`, `Dependency`, `FundingTranche`), — то же, если EF не может использовать существующий конструктор. Комментарий: «For EF Core materialization only; does not raise domain events.» Non-nullable свойства в таких конструкторах инициализируются `null!`.
2. `DomainEvent` без системных часов: убрать дефолты `EventId = Guid.CreateVersion7()` и `OccurredAt = DateTime.UtcNow`. Поля проставляет UoW при записи в outbox через `with` из `TimeProvider` (фаза 2). Обновить XML-комментарии `DomainEvent` / `IDomainEvent`: до сохранения поля пустые. Проверить, что ни один тест и ни один handler не опирается на эти поля до сохранения.
3. Тесты на `FakeTimeProvider`: заменить `DateTime.UtcNow` в `tests/` (`TestData`, `ProjectTests`, `MilestoneTests`, `CommandTestBase` и др.) на значения из `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing` уже подключён в Application.Tests).
4. Исключение конфликта версий в Application: `ConcurrencyConflictException` (агрегат изменён кем-то другим с момента загрузки). Его будет выбрасывать UoW (фаза 2).

**Ревью:** domain/application тесты зелёные, поведение не изменилось.

## Фаза 1 — Проект Infrastructure, DbContext, маппинг, миграция

1. Новый проект `src/ProjectManagementSystem.Infrastructure` (ссылки: Application, Domain), добавить в `.slnx`. Пакеты: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`.
2. `AppDbContext` — write-side, схема по умолчанию `dbo`. Конфигурации — по одному `IEntityTypeConfiguration<T>` на агрегат в `Persistence/Configurations/`.
3. Маппинг агрегатов:
   - `Project`: `Roles` и `TeamMembers` — `OwnsMany` (сущности с Id); `TeamMember.HourlyRates` — вложенный `OwnsMany` (shadow key); `Currency` — `ComplexProperty`/owned (колонка `CurrencyCode`).
   - `WorkPackage`: `Tasks` — `OwnsMany`.
   - `ScheduleNetwork`: `Dependencies` — `OwnsMany` (shadow key или составной ключ из `ProjectId + PredecessorTaskId + SuccessorTaskId`).
   - `TaskTimeLog`: `Entries` — `OwnsMany`.
   - `ProjectFunding`: `Tranches` — `OwnsMany` (shadow key).
   - `Milestone.TaskIds`, `Project.WorkPackageIds` (`HashSet<Guid>`) — OwnsMany не применим к примитивам. По умолчанию — primitive collection EF Core (JSON-колонка). Если при реализации это окажется неудобным — остановиться и обсудить, не решать самостоятельно.
     **Решено при реализации:** EF Core не принимает `HashSet<Guid>` как primitive collection (только массивы и списки), поэтому поля `_workPackageIds` и `_taskIds` в домене стали `List<Guid>`; уникальность по-прежнему проверяют методы агрегатов.
   - `Risk`, `Employee` — плоские таблицы.
   - Маппинг через backing fields (`_teamMembers`, `_tasks`, …) с `PropertyAccessMode.Field`.
   - `Version` — `IsConcurrencyToken()`; доступ к private setter через поле/`Property(...)`.
   - Все `decimal` — явная точность (`HasPrecision(18, 2)` для денег, `(9, 2)` для часов и т. п.).
   - Enums — как `int` (у них явные значения в Domain.Shared).
   - Unique index `Employees.NormalizedEmail`.
   - `DomainEvents` агрегатов — `Ignore`.
4. Таблица `OutboxMessages`: `Sequence` (bigint identity, PK кластерный), `EventId` (unique), `EventType` (стабильное имя), `Payload` (nvarchar(max), JSON), `OccurredAt`, `ProcessedAt`, `Attempts`, `NextAttemptAt`, `LastError`, `ParkedAt`. Индекс по `(ProcessedAt, ParkedAt, Sequence)`.
5. Таблица `ParkedEventTypes`: `EventType` (PK), `ParkedAt`, `Reason`.
6. Таблица `Notifications`: `Id`, `IdempotencyKey` (unique), `ProjectId`, `RecipientEmployeeId` (null = руководство проекта), `Message`, `CreatedAt`, `ReadAt` (null). Позже (миграция `AddNotificationDetails`) добавлены `Kind`, `Title`, `WorkPackageId`, `TaskId`.
7. Начальная миграция `Initial`.
8. `DependencyInjection.AddInfrastructure(IServiceCollection, IConfiguration)`: DbContext со строкой подключения `ConnectionStrings:Default`, `EnableRetryOnFailure()`.
9. **Добавлено при ревью:** проект `src/ProjectManagementSystem.Migrator` — консольное приложение на Generic Host, применяет миграции (`dotnet run --project src/ProjectManagementSystem.Migrator`; с `--script` печатает идемпотентный SQL без подключения к базе). Настройки: `appsettings.json` → `appsettings.{Environment}.json` → user secrets (Development) → переменные окружения → командная строка. Он же startup-проект для `dotnet ef` (`--project src/ProjectManagementSystem.Infrastructure --startup-project src/ProjectManagementSystem.Migrator`). Регистрирует только `AddPersistence` (DbContext), не весь `AddInfrastructure`.

**Ревью:** миграция сгенерирована и собирается; Claude Code приводит сгенерированный SQL (`dotnet ef migrations script`) для проверки. Применение на локальный SQL Server — автор.

## Фаза 2 — Репозитории и Unit of Work

1. `EfRepository<TAggregate> : IRepository<TAggregate>` — один generic-класс:
   - `FindAsync` — загрузка агрегата целиком (owned-коллекции грузятся автоматически).
   - `ExistsAsync` — `AnyAsync(predicate)`.
   - `Add` / `Update` / `Delete` — регистрируют агрегат в UoW (список изменённых агрегатов текущего scope).
2. `EfUnitOfWork : IUnitOfWork`, `SaveChangesAsync`:
   1. Для каждого агрегата из `Update` — `Version + 1` (через EF entry, т. к. setter private).
   2. Собрать `DomainEvents` из всех зарегистрированных агрегатов (включая удалённые), в порядке их поднятия; каждому событию проставить `EventId = Guid.CreateVersion7(now)` и `OccurredAt = now` через `with` (`now` — из `TimeProvider`); сериализовать в `OutboxMessages`.
   3. `ClearDomainEvents()` у агрегатов.
   4. `DbContext.SaveChangesAsync` — одна неявная транзакция (без `BeginTransaction`, чтобы не конфликтовать с retrying execution strategy).
   5. После успешного commit, если были добавлены outbox-сообщения, — `OutboxSignal.Notify()`.
   6. Трансляция ошибок: `DbUpdateConcurrencyException` → `ConcurrencyConflictException`; `DbUpdateException` с unique violation (SQL 2601/2627) на индексе `NormalizedEmail` → та же ошибка, что выбрасывает `EmployeeService` при занятом email.
3. Сериализация событий: `System.Text.Json` + `EventTypeRegistry` — явное соответствие «стабильное имя ↔ тип» (имя = имя класса события; регистрация сканированием сборки Domain по `IDomainEvent` с проверкой уникальности имён при старте). Не использовать `AssemblyQualifiedName`. Проверить round-trip всех событий, включая вложенные record'ы (`Dependency`) и `DateOnly`.
4. Регистрация в DI: `IRepository<>` → `EfRepository<>`, `IUnitOfWork` → `EfUnitOfWork` (scoped), `OutboxSignal` (singleton).

5. **Решено при реализации:**
   - `SaveChangesAsync` отклоняет (`InvalidOperationException`) агрегат, который поднял события, но не был передан в `Add` / `Update` / `Delete`: иначе его события потерялись бы, а версия не выросла.
   - Ошибка занятого email вынесена в `EmployeeService.EmailIsTaken(...)` (public static), её же бросает UoW при нарушении индекса `UX_Employees_NormalizedEmail`.
   - Unit-тесты Infrastructure без базы — отдельный проект `tests/ProjectManagementSystem.Infrastructure.Tests`; он входит в список проектов, которые запускает CI (наравне с `Domain.Tests` и `Application.Tests`).

**Ревью:** unit-тесты сериализатора (round-trip каждого типа события).

## Фаза 3 — Outbox processor

1. `OutboxSignal` — singleton: `Channel<bool>` ёмкостью 1, `FullMode = DropWrite`; `Notify()` / `WaitAsync(ct)`. Сигналы схлопываются, сигнал во время обработки не теряется.
2. `OutboxProcessor : BackgroundService`:

   ```
   при старте: Drain()
   loop:
       nextAttemptAt = Drain()
       if nextAttemptAt is null:  await signal.WaitAsync()                                   // в простое к БД не ходим
       else:                      await WhenAny(signal.WaitAsync(), Task.Delay(nextAttemptAt - now, timeProvider))
   ```

3. `Drain()`: в цикле берёт самое раннее необработанное, не запаркованное сообщение по `Sequence`, тип которого не в `ParkedEventTypes`:
   - если `NextAttemptAt > now` — вернуть `NextAttemptAt` (очередь ждёт это сообщение, строгий порядок);
   - иначе обработать в новом DI-scope:
     1. десериализовать, вызвать `IDomainEventDispatcher.DispatchAsync` (все handlers события по очереди);
     2. после того как все handlers отработали — проставить `ProcessedAt` и `SaveChangesAsync`. Тот же `SaveChanges` сохраняет уведомления, добавленные через `INotificationSender`, — для handlers, которые только уведомляют, обработка атомарна. Handlers, меняющие агрегаты, сами вызывают `SaveChangesAsync` раньше (одна транзакция — один агрегат); при сбое до отметки `ProcessedAt` сообщение будет доставлено повторно — handlers идемпотентны.
     3. при исключении — в отдельном scope: `Attempts + 1`, `LastError`, `NextAttemptAt = now + backoff` (10 с → 1 мин → 10 мин → 1 ч; значения в options). После `MaxAttempts` (options, по умолчанию 5) — запарковать тип события: запись в `ParkedEventTypes`, `ParkedAt` у этого сообщения, лог Error.
   - если сообщений нет — вернуть `null`.
4. Сбой самого `Drain()` (база недоступна, не проснулась, ошибка чтения таблицы — то есть ошибка вне обработки конкретного сообщения): цикл processor'а не должен падать — необработанное исключение в `BackgroundService` останавливает весь хост. Поэтому:
   - каждый вызов `Drain()`, включая проход при старте, обёрнут в `try/catch`;
   - `OperationCanceledException` при остановке приложения (`stoppingToken`) — нормальный выход из цикла, не ошибка;
   - любое другое исключение — лог Error и ожидание по отдельному backoff сбоев `Drain()` (5 с → 30 с → 2 мин → 10 мин, дальше 10 мин; значения в options), после чего — новая попытка `Drain()`. Счётчик сбоев сбрасывается после первого успешного `Drain()`;
   - во время этого ожидания сигналы не прерывают его (иначе при недоступной базе каждая команда вызывала бы новую неудачную попытку). Записка остаётся в ящике и не теряется: после ожидания `Drain()` всё равно читает таблицу целиком;
   - этот backoff не связан с `Attempts` / `NextAttemptAt` сообщений: сообщение не помечается неудачным, если упало чтение очереди, а не его handler.
5. Очистка: в конце `Drain()` удалить сообщения с `ProcessedAt` старше `RetentionPeriod` (options, по умолчанию 7 дней) — без отдельного таймера.
6. Всё время — только через `TimeProvider` (`Task.Delay(..., timeProvider)`), чтобы цикл тестировался `FakeTimeProvider`.
7. Регистрация `OutboxProcessor` и `OutboxOptions` в `AddInfrastructure`.

8. **Решено при реализации:**
   - Цикл (`OutboxProcessor`) отделён от работы с базой (`IOutboxDrainer` → `OutboxDrainer`), поэтому цикл тестируется без БД.
   - Списки backoff из конфигурации **заменяют** значения по умолчанию (обычный bind дописал бы их к списку по умолчанию); options валидируются.
   - Сообщения того же типа после парковки отфильтровываются по `ParkedEventTypes`; `ParkedAt` проставляется только у сообщения, исчерпавшего попытки.

**Ревью:** unit-тесты processor'а на `FakeTimeProvider` и in-memory заменах там, где это возможно без БД (сигнал, backoff, отсутствие обращений к хранилищу в простое; сбой `Drain()` не останавливает цикл, следующая попытка — по backoff сбоев, после успеха счётчик сброшен; отмена `stoppingToken` завершает цикл без ошибки).

## Фаза 4 — Уведомления

1. `NotificationSender : INotificationSender` — добавляет строку в `Notifications` через текущий `DbContext` без собственного `SaveChanges` (сохранит processor вместе с `ProcessedAt`). Перед добавлением проверяет, нет ли уже уведомления с тем же `IdempotencyKey` в БД и среди добавленных в текущем scope; если есть — ничего не делает.
2. Страница уведомлений и алерт — не в этом плане (вместе с фронтом).

**Ревью:** повторная доставка не создаёт дубликатов.

## Фаза 5 — Интеграционные тесты (только локально)

1. Проект `tests/ProjectManagementSystem.Infrastructure.IntegrationTests` (xUnit, `Respawn`).
2. Строка подключения — из user secrets / переменной окружения (не из репозитория). Если её нет — тесты пропускаются (skip), а не падают.
3. Фикстура прогона: создать базу с уникальным именем, применить миграции; в конце — удалить базу. Между тестами — очистка данных через Respawn.
4. CI запускает только `Domain.Tests` и `Application.Tests` (явным списком проектов).

### Фаза 5a — элементарные тесты записи полей (начать с них)

Для каждого агрегата: создать экземпляр с заполненными полями (включая owned-коллекции и `HashSet<Guid>`), сохранить через репозиторий и UoW, загрузить в новом scope / новом `DbContext` и проверить, что каждое поле записалось и прочиталось с тем же значением (`decimal` с точностью, `DateOnly`, enums, nullable-поля как `null` и как значение, `Version`). Один тест на агрегат, плюс тест, что материализация не поднимает доменные события.

**Ревью (стоп):** код собирается; автор прогоняет локально, только после этого — 5b.

### Фаза 5b — поведенческие тесты

5. Тесты:
   - optimistic concurrency: две параллельные загрузки, обе меняют только owned-коллекцию → вторая получает `ConcurrencyConflictException`;
   - уникальный email: две параллельные регистрации → одна успешна, вторая получает доменную ошибку;
   - outbox атомарен: при сбое `SaveChanges` нет ни агрегата, ни сообщения; при успехе есть оба;
   - processor: строгий порядок; повтор после сбоя; парковка типа после `MaxAttempts`, остальные сообщения обрабатываются;
   - сквозной сценарий eventual consistency `RegisterWorkPackageInProject`: создание пакета → регистрация в проекте; и ветка компенсации — проект закрыт до обработки события → пакет отменён.

**Ревью:** код собирается; прогон на локальном SQL Server — автор.

## Вне рамок плана (обсудим позже)

Миграции при деплое; host-проект (Web API + Blazor Server/WASM); аутентификация; CI/CD в Azure; read-модели в схеме `read`; страница и алерт уведомлений.
