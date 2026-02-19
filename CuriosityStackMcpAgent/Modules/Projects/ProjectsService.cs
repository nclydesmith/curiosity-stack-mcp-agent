using CuriosityStack.Mcp.Core.Storage;

namespace CuriosityStack.Mcp.Projects;

public interface IProjectsService
{
    Task<object> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<object> GetDeadlinesAsync(int days, CancellationToken cancellationToken = default);
    Task<object> GetVelocityAsync(int days, CancellationToken cancellationToken = default);
    Task<object> GetRiskFlagsAsync(CancellationToken cancellationToken = default);
    Task<string> CreateAsync(string name, string owner, DateTime? dueDateUtc, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(string projectId, string status, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(string projectId, CancellationToken cancellationToken = default);
}

public sealed class ProjectsService : IProjectsService
{
    private readonly ISqliteStore _store;

    public ProjectsService(ISqliteStore store)
    {
        _store = store;
    }

    public async Task<object> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var items = await _store.QueryAsync(
            "SELECT Id, Name, Status, Owner, DueDateUtc, UpdatedAtUtc FROM ProjectRegistry WHERE IsArchived = 0 ORDER BY UpdatedAtUtc DESC;",
            r => new
            {
                Id = r.GetString(0),
                Name = r.GetString(1),
                Status = r.GetString(2),
                Owner = r.GetString(3),
                DueDateUtc = r.IsDBNull(4) ? (DateTime?)null : DateTime.Parse(r.GetString(4)),
                UpdatedAtUtc = DateTime.Parse(r.GetString(5)),
            },
            cancellationToken: cancellationToken);

        return new { count = items.Count, projects = items };
    }

    public async Task<object> GetDeadlinesAsync(int days, CancellationToken cancellationToken = default)
    {
        var until = DateTime.UtcNow.AddDays(Math.Abs(days));
        var milestones = await _store.QueryAsync(
            """
            SELECT m.Id, m.ProjectId, m.Title, m.DueDateUtc, p.Name
            FROM Milestones m
            JOIN ProjectRegistry p ON p.Id = m.ProjectId
            WHERE p.IsArchived = 0 AND m.IsCompleted = 0 AND m.DueDateUtc <= @until
            ORDER BY m.DueDateUtc ASC;
            """,
            r => new
            {
                Id = r.GetString(0),
                ProjectId = r.GetString(1),
                Title = r.GetString(2),
                DueDateUtc = DateTime.Parse(r.GetString(3)),
                ProjectName = r.GetString(4),
            },
            new Dictionary<string, object?> { ["until"] = until.ToString("O") },
            cancellationToken);

        return new { windowDays = days, count = milestones.Count, deadlines = milestones };
    }

    public async Task<object> GetVelocityAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var completed = await _store.QuerySingleAsync(
            "SELECT COUNT(*) FROM Milestones WHERE IsCompleted = 1 AND DueDateUtc >= @since;",
            r => r.GetInt32(0),
            new Dictionary<string, object?> { ["since"] = since.ToString("O") },
            cancellationToken);

        var active = await _store.QuerySingleAsync(
            "SELECT COUNT(*) FROM ProjectRegistry WHERE IsArchived = 0;",
            r => r.GetInt32(0),
            cancellationToken: cancellationToken);

        return new
        {
            windowDays = days,
            completedMilestones = completed,
            activeProjects = active,
            velocityPerWeek = days == 0 ? 0 : Math.Round((double)completed / days * 7.0, 2),
        };
    }

    public async Task<object> GetRiskFlagsAsync(CancellationToken cancellationToken = default)
    {
        var risks = await _store.QueryAsync(
            """
            SELECT r.Id, r.ProjectId, p.Name, r.Severity, r.Description, r.CreatedAtUtc
            FROM RiskFlags r
            JOIN ProjectRegistry p ON p.Id = r.ProjectId
            WHERE p.IsArchived = 0 AND r.IsOpen = 1
            ORDER BY r.CreatedAtUtc DESC;
            """,
            r => new
            {
                Id = r.GetString(0),
                ProjectId = r.GetString(1),
                ProjectName = r.GetString(2),
                Severity = r.GetString(3),
                Description = r.GetString(4),
                CreatedAtUtc = DateTime.Parse(r.GetString(5)),
            },
            cancellationToken: cancellationToken);

        return new { count = risks.Count, openRisks = risks };
    }

    public async Task<string> CreateAsync(string name, string owner, DateTime? dueDateUtc, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow.ToString("O");

        await _store.ExecuteAsync(
            """
            INSERT INTO ProjectRegistry (Id, Name, Status, Owner, DueDateUtc, IsArchived, CreatedAtUtc, UpdatedAtUtc)
            VALUES (@id, @name, 'Active', @owner, @dueDateUtc, 0, @createdAtUtc, @updatedAtUtc);
            """,
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["name"] = name,
                ["owner"] = owner,
                ["dueDateUtc"] = dueDateUtc?.ToString("O"),
                ["createdAtUtc"] = now,
                ["updatedAtUtc"] = now,
            },
            cancellationToken);

        return id;
    }

    public async Task<bool> UpdateStatusAsync(string projectId, string status, CancellationToken cancellationToken = default)
    {
        var rows = await _store.ExecuteAsync(
            "UPDATE ProjectRegistry SET Status = @status, UpdatedAtUtc = @updatedAtUtc WHERE Id = @id AND IsArchived = 0;",
            new Dictionary<string, object?>
            {
                ["id"] = projectId,
                ["status"] = status,
                ["updatedAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return rows > 0;
    }

    public async Task<bool> ArchiveAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var rows = await _store.ExecuteAsync(
            "UPDATE ProjectRegistry SET IsArchived = 1, UpdatedAtUtc = @updatedAtUtc WHERE Id = @id;",
            new Dictionary<string, object?>
            {
                ["id"] = projectId,
                ["updatedAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return rows > 0;
    }
}
