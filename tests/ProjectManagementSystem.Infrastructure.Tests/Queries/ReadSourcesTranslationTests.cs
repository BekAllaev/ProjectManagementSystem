using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectManagementSystem.Domain.Projects;
using ProjectManagementSystem.Domain.Risks;
using ProjectManagementSystem.Infrastructure.Persistence;
using ProjectManagementSystem.Infrastructure.Queries;

namespace ProjectManagementSystem.Infrastructure.Tests.Queries;

/// <summary>
/// The query handlers run against SQLite in the other tests; production runs on SQL Server. Every read of the handlers
/// is in <see cref="ReadSources"/>, so translating each of them for SQL Server, which needs no connection, catches a
/// query that only SQL Server would reject.
/// </summary>
public sealed class ReadSourcesTranslationTests : IDisposable
{
    private readonly AppDbContext _dbContext = new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer("Server=localhost;Database=NotUsed")
        .Options);

    public static TheoryData<string> Sources() =>
        new(typeof(ReadSources).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(IQueryable<>))
            .Select(Describe));

    [Theory]
    [MemberData(nameof(Sources))]
    public void Source_translates_to_sql_server(string source)
    {
        var method = typeof(ReadSources).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => Describe(m) == source);
        var arguments = method.GetParameters().Select(p => p.Position == 0 ? _dbContext : Sample(p.ParameterType)).ToArray();

        var query = (IQueryable)method.Invoke(null, arguments)!;

        // ToQueryString throws if the query cannot be translated.
        Assert.Contains("SELECT", query.ToQueryString());
    }

    public void Dispose() => _dbContext.Dispose();

    private static string Describe(MethodInfo method) =>
        $"{method.Name}({string.Join(", ", method.GetParameters().Skip(1).Select(p => p.ParameterType.Name))})";

    private static object? Sample(Type type) => type switch
    {
        _ when type == typeof(Guid) => Guid.NewGuid(),
        _ when type == typeof(Guid?) => Guid.NewGuid(),
        _ when type == typeof(IReadOnlyCollection<Guid>) => new[] { Guid.NewGuid(), Guid.NewGuid() },
        _ when type == typeof(string) => "text",
        _ when type == typeof(DateOnly) => new DateOnly(2026, 3, 2),
        _ when type == typeof(ProjectState?) => ProjectState.InProgress,
        _ when type == typeof(RiskStatus?) => RiskStatus.Open,
        _ => throw new NotSupportedException($"No sample value for {type}."),
    };
}
