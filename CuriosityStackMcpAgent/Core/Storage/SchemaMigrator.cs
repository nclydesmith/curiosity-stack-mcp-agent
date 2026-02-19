using Microsoft.Data.Sqlite;

namespace CuriosityStack.Mcp.Core.Storage;

public interface ISchemaMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public sealed class SchemaMigrator : ISchemaMigrator
{
    private readonly ISqliteStore _store;

    public SchemaMigrator(ISqliteStore store)
    {
        _store = store;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await _store.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Version INTEGER PRIMARY KEY,
                AppliedAtUtc TEXT NOT NULL
            );
            """, cancellationToken: cancellationToken);

        var applied = await _store.QueryAsync(
            "SELECT Version FROM SchemaMigrations ORDER BY Version;",
            r => r.GetInt32(0),
            cancellationToken: cancellationToken);

        var known = applied.ToHashSet();
        var migrations = GetMigrations();

        foreach (var migration in migrations)
        {
            if (known.Contains(migration.Version))
            {
                continue;
            }

            foreach (var sql in migration.SqlStatements)
            {
                await _store.ExecuteAsync(sql, cancellationToken: cancellationToken);
            }

            await _store.ExecuteAsync(
                "INSERT INTO SchemaMigrations (Version, AppliedAtUtc) VALUES (@version, @appliedAtUtc);",
                new Dictionary<string, object?>
                {
                    ["version"] = migration.Version,
                    ["appliedAtUtc"] = DateTime.UtcNow.ToString("O"),
                },
                cancellationToken);
        }
    }

    private static List<SqlMigration> GetMigrations()
    {
        return
        [
            new SqlMigration(1,
            [
                """
                CREATE TABLE IF NOT EXISTS Accounts (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    AccountType TEXT NOT NULL,
                    Balance REAL NOT NULL,
                    Currency TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Positions (
                    Id TEXT PRIMARY KEY,
                    Symbol TEXT NOT NULL,
                    Quantity REAL NOT NULL,
                    MarketValue REAL NOT NULL,
                    MarginExposure REAL NOT NULL,
                    AsOfUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS CashFlowSnapshots (
                    Id TEXT PRIMARY KEY,
                    SnapshotDateUtc TEXT NOT NULL,
                    Inflows REAL NOT NULL,
                    Outflows REAL NOT NULL,
                    BurnRate REAL NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS FinanceManualEntries (
                    Id TEXT PRIMARY KEY,
                    Category TEXT NOT NULL,
                    Amount REAL NOT NULL,
                    Notes TEXT,
                    CreatedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS WeightLogs (
                    Id TEXT PRIMARY KEY,
                    WeightKg REAL NOT NULL,
                    LoggedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS TrainingSessions (
                    Id TEXT PRIMARY KEY,
                    SessionType TEXT NOT NULL,
                    DurationMinutes INTEGER NOT NULL,
                    Intensity INTEGER NOT NULL,
                    Notes TEXT,
                    StartedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS RecoveryMetrics (
                    Id TEXT PRIMARY KEY,
                    Score INTEGER NOT NULL,
                    RestingHeartRate INTEGER,
                    SleepHours REAL,
                    LoggedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS ProjectRegistry (
                    Id TEXT PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Status TEXT NOT NULL,
                    Owner TEXT NOT NULL,
                    DueDateUtc TEXT,
                    IsArchived INTEGER NOT NULL DEFAULT 0,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Milestones (
                    Id TEXT PRIMARY KEY,
                    ProjectId TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    DueDateUtc TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CreatedAtUtc TEXT NOT NULL,
                    FOREIGN KEY(ProjectId) REFERENCES ProjectRegistry(Id)
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS RiskFlags (
                    Id TEXT PRIMARY KEY,
                    ProjectId TEXT NOT NULL,
                    Severity TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    IsOpen INTEGER NOT NULL DEFAULT 1,
                    CreatedAtUtc TEXT NOT NULL,
                    FOREIGN KEY(ProjectId) REFERENCES ProjectRegistry(Id)
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Entries (
                    Id TEXT PRIMARY KEY,
                    Title TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Tags TEXT,
                    CreatedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS Decisions (
                    Id TEXT PRIMARY KEY,
                    DecisionText TEXT NOT NULL,
                    Rationale TEXT NOT NULL,
                    ExpectedOutcome TEXT,
                    CreatedAtUtc TEXT NOT NULL
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS OutcomeReviews (
                    Id TEXT PRIMARY KEY,
                    DecisionId TEXT NOT NULL,
                    OutcomeText TEXT NOT NULL,
                    Score INTEGER,
                    ReviewedAtUtc TEXT NOT NULL,
                    FOREIGN KEY(DecisionId) REFERENCES Decisions(Id)
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS ApprovalRecords (
                    TokenId TEXT PRIMARY KEY,
                    Domain TEXT NOT NULL,
                    ToolName TEXT NOT NULL,
                    Scope INTEGER NOT NULL,
                    ExpiresAtUtc TEXT NOT NULL,
                    ApprovedAtUtc TEXT NOT NULL,
                    IsConsumed INTEGER NOT NULL DEFAULT 0
                );
                """,
                """
                CREATE TABLE IF NOT EXISTS AuditLog (
                    Id TEXT PRIMARY KEY,
                    CorrelationId TEXT NOT NULL,
                    Domain TEXT NOT NULL,
                    ToolName TEXT NOT NULL,
                    Scope INTEGER NOT NULL,
                    SideEffects TEXT NOT NULL,
                    Succeeded INTEGER NOT NULL,
                    FailureReason TEXT,
                    DurationMs INTEGER NOT NULL,
                    MetadataJson TEXT,
                    OccurredAtUtc TEXT NOT NULL
                );
                """
            ])
        ];
    }
}

internal sealed record SqlMigration(int Version, IReadOnlyList<string> SqlStatements);
