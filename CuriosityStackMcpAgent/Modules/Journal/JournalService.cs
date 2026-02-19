using CuriosityStack.Mcp.Core.Storage;

namespace CuriosityStack.Mcp.Journal;

public interface IJournalService
{
    Task<string> AddEntryAsync(string title, string content, string? tags, CancellationToken cancellationToken = default);
    Task<string> LogDecisionAsync(string decisionText, string rationale, string? expectedOutcome, CancellationToken cancellationToken = default);
    Task<object> PatternAnalysisAsync(CancellationToken cancellationToken = default);
    Task<object> GoalAlignmentAsync(CancellationToken cancellationToken = default);
}

public sealed class JournalService : IJournalService
{
    private readonly ISqliteStore _store;

    public JournalService(ISqliteStore store)
    {
        _store = store;
    }

    public async Task<string> AddEntryAsync(string title, string content, string? tags, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await _store.ExecuteAsync(
            "INSERT INTO Entries (Id, Title, Content, Tags, CreatedAtUtc) VALUES (@id, @title, @content, @tags, @createdAtUtc);",
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["title"] = title,
                ["content"] = content,
                ["tags"] = tags,
                ["createdAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return id;
    }

    public async Task<string> LogDecisionAsync(string decisionText, string rationale, string? expectedOutcome, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await _store.ExecuteAsync(
            "INSERT INTO Decisions (Id, DecisionText, Rationale, ExpectedOutcome, CreatedAtUtc) VALUES (@id, @decisionText, @rationale, @expectedOutcome, @createdAtUtc);",
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["decisionText"] = decisionText,
                ["rationale"] = rationale,
                ["expectedOutcome"] = expectedOutcome,
                ["createdAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return id;
    }

    public async Task<object> PatternAnalysisAsync(CancellationToken cancellationToken = default)
    {
        var entryCount = await _store.QuerySingleAsync(
            "SELECT COUNT(*) FROM Entries;",
            r => r.GetInt32(0),
            cancellationToken: cancellationToken);

        var decisionCount = await _store.QuerySingleAsync(
            "SELECT COUNT(*) FROM Decisions;",
            r => r.GetInt32(0),
            cancellationToken: cancellationToken);

        var topTags = await _store.QueryAsync(
            "SELECT Tags FROM Entries WHERE Tags IS NOT NULL AND TRIM(Tags) <> '';",
            r => r.GetString(0),
            cancellationToken: cancellationToken);

        var tagHistogram = topTags
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => new { tag = g.Key, count = g.Count() })
            .ToList();

        return new
        {
            entries = entryCount,
            decisions = decisionCount,
            topTags = tagHistogram,
        };
    }

    public async Task<object> GoalAlignmentAsync(CancellationToken cancellationToken = default)
    {
        var latestEntries = await _store.QueryAsync(
            "SELECT Title, Content, CreatedAtUtc FROM Entries ORDER BY CreatedAtUtc DESC LIMIT 20;",
            r => new
            {
                Title = r.GetString(0),
                Content = r.GetString(1),
                CreatedAtUtc = DateTime.Parse(r.GetString(2)),
            },
            cancellationToken: cancellationToken);

        var score = latestEntries.Count == 0
            ? 0
            : latestEntries.Count(e => e.Content.Contains("goal", StringComparison.OrdinalIgnoreCase) || e.Title.Contains("goal", StringComparison.OrdinalIgnoreCase)) * 100 / latestEntries.Count;

        return new
        {
            alignmentScore = score,
            sampleSize = latestEntries.Count,
            note = "Heuristic based on explicit goal references in recent entries.",
        };
    }
}
