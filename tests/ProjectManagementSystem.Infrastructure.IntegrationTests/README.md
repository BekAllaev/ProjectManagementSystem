# Integration tests of the infrastructure

Run only locally, against an installed SQL Server; not in CI and without containers.

## Setup (once)

Give the tests a server. The database name in the string is ignored: every run creates its own database
`ProjectManagementSystem_IntegrationTests_<guid>`, applies the migrations and drops it at the end.

```
dotnet user-secrets set "IntegrationTests:SqlServer" "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True" --project tests/ProjectManagementSystem.Infrastructure.IntegrationTests
```

or the environment variable `IntegrationTests__SqlServer`. The account needs the right to create and drop databases.

Without the setting every test is skipped, not failed.

## Run

```
dotnet test tests/ProjectManagementSystem.Infrastructure.IntegrationTests
```

Data is deleted with Respawn before every test; the tests of one run share the database and run one by one.

CI runs only the unit test projects, listed explicitly: `Domain.Tests`, `Application.Tests`, `Infrastructure.Tests`.
