using CuriosityStack.Mcp.Core.Storage;

namespace CuriosityStack.Mcp.Health;

public interface IHealthService
{
    Task<object> GetWeightTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<object> GetTrainingLoadAsync(int days, CancellationToken cancellationToken = default);
    Task<object> GetRecoveryScoreAsync(CancellationToken cancellationToken = default);
    Task<string> LogWeightAsync(decimal weightKg, DateTime loggedAtUtc, CancellationToken cancellationToken = default);
    Task<string> LogTrainingSessionAsync(string sessionType, int durationMinutes, int intensity, string? notes, DateTime startedAtUtc, CancellationToken cancellationToken = default);
}

public sealed class HealthService : IHealthService
{
    private readonly ISqliteStore _store;

    public HealthService(ISqliteStore store)
    {
        _store = store;
    }

    public async Task<object> GetWeightTrendAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var weights = await _store.QueryAsync(
            "SELECT WeightKg, LoggedAtUtc FROM WeightLogs WHERE LoggedAtUtc >= @since ORDER BY LoggedAtUtc ASC;",
            r => new
            {
                WeightKg = Convert.ToDecimal(r.GetDouble(0)),
                LoggedAtUtc = DateTime.Parse(r.GetString(1)),
            },
            new Dictionary<string, object?> { ["since"] = since.ToString("O") },
            cancellationToken);

        var start = weights.Count > 0 ? weights[0].WeightKg : 0m;
        var end = weights.Count > 0 ? weights[^1].WeightKg : 0m;

        return new
        {
            periodDays = days,
            sampleCount = weights.Count,
            startWeightKg = start,
            endWeightKg = end,
            deltaKg = end - start,
            points = weights,
        };
    }

    public async Task<object> GetTrainingLoadAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var sessions = await _store.QueryAsync(
            "SELECT SessionType, DurationMinutes, Intensity, StartedAtUtc FROM TrainingSessions WHERE StartedAtUtc >= @since ORDER BY StartedAtUtc DESC;",
            r => new
            {
                SessionType = r.GetString(0),
                DurationMinutes = r.GetInt32(1),
                Intensity = r.GetInt32(2),
                StartedAtUtc = DateTime.Parse(r.GetString(3)),
            },
            new Dictionary<string, object?> { ["since"] = since.ToString("O") },
            cancellationToken);

        var loadScore = sessions.Sum(s => s.DurationMinutes * s.Intensity);
        return new
        {
            periodDays = days,
            sessions = sessions.Count,
            loadScore,
            recentSessions = sessions,
        };
    }

    public async Task<object> GetRecoveryScoreAsync(CancellationToken cancellationToken = default)
    {
        var recovery = await _store.QuerySingleAsync(
            "SELECT Score, RestingHeartRate, SleepHours, LoggedAtUtc FROM RecoveryMetrics ORDER BY LoggedAtUtc DESC LIMIT 1;",
            r => new
            {
                Score = r.GetInt32(0),
                RestingHeartRate = r.IsDBNull(1) ? (int?)null : r.GetInt32(1),
                SleepHours = r.IsDBNull(2) ? (decimal?)null : Convert.ToDecimal(r.GetDouble(2)),
                LoggedAtUtc = (DateTime?)DateTime.Parse(r.GetString(3)),
            },
            cancellationToken: cancellationToken);

        return recovery ?? new { Score = 0, RestingHeartRate = (int?)null, SleepHours = (decimal?)null, LoggedAtUtc = (DateTime?)null };
    }

    public async Task<string> LogWeightAsync(decimal weightKg, DateTime loggedAtUtc, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await _store.ExecuteAsync(
            "INSERT INTO WeightLogs (Id, WeightKg, LoggedAtUtc) VALUES (@id, @weightKg, @loggedAtUtc);",
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["weightKg"] = weightKg,
                ["loggedAtUtc"] = loggedAtUtc.ToString("O"),
            },
            cancellationToken);

        return id;
    }

    public async Task<string> LogTrainingSessionAsync(string sessionType, int durationMinutes, int intensity, string? notes, DateTime startedAtUtc, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await _store.ExecuteAsync(
            """
            INSERT INTO TrainingSessions (Id, SessionType, DurationMinutes, Intensity, Notes, StartedAtUtc)
            VALUES (@id, @sessionType, @durationMinutes, @intensity, @notes, @startedAtUtc);
            """,
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["sessionType"] = sessionType,
                ["durationMinutes"] = durationMinutes,
                ["intensity"] = intensity,
                ["notes"] = notes,
                ["startedAtUtc"] = startedAtUtc.ToString("O"),
            },
            cancellationToken);

        return id;
    }
}
