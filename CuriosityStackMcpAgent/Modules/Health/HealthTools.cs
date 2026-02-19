using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Observability;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Health;

[McpServerToolType]
public sealed class HealthTools
{
    private readonly IHealthService _health;
    private readonly IToolExecutionRunner _runner;

    public HealthTools(IHealthService health, IToolExecutionRunner runner)
    {
        _health = health;
        _runner = runner;
    }

    [Description("Get weight trend for recent days.")]
    [McpServerTool(Name = "health.weight_trend")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> WeightTrendAsync(
        [Description("Number of days") ] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("health", "health.weight_trend", policy, ctx, ct => _health.GetWeightTrendAsync(days, ct), cancellationToken);
    }

    [Description("Get training load score over a period.")]
    [McpServerTool(Name = "health.training_load")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> TrainingLoadAsync(
        [Description("Number of days")] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("health", "health.training_load", policy, ctx, ct => _health.GetTrainingLoadAsync(days, ct), cancellationToken);
    }

    [Description("Get latest recovery score.")]
    [McpServerTool(Name = "health.recovery_score")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> RecoveryScoreAsync(CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("health", "health.recovery_score", policy, ctx, ct => _health.GetRecoveryScoreAsync(ct), cancellationToken);
    }

    [Description("Write a new weight log.")]
    [McpServerTool(Name = "health.log_weight")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Writes to WeightLogs.", false)]
    public Task<string> LogWeightAsync(
        [Description("Weight in kg")] decimal weightKg,
        [Description("Timestamp UTC, optional")] DateTime? loggedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.Write, ApprovalLevel.None, "Writes to WeightLogs.", false);
        return _runner.RunAsync("health", "health.log_weight", policy, ctx, async ct =>
        {
            var id = await _health.LogWeightAsync(weightKg, loggedAtUtc ?? DateTime.UtcNow, ct);
            return new { weightLogId = id };
        }, cancellationToken);
    }

    [Description("Write a training session entry.")]
    [McpServerTool(Name = "health.log_training_session")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Writes to TrainingSessions.", false)]
    public Task<string> LogTrainingSessionAsync(
        [Description("Session type")] string sessionType,
        [Description("Duration in minutes")] int durationMinutes,
        [Description("Intensity 1-10")] int intensity,
        [Description("Optional notes")] string? notes = null,
        [Description("UTC start time, optional")] DateTime? startedAtUtc = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.Write, ApprovalLevel.None, "Writes to TrainingSessions.", false);
        return _runner.RunAsync("health", "health.log_training_session", policy, ctx, async ct =>
        {
            var id = await _health.LogTrainingSessionAsync(sessionType, durationMinutes, intensity, notes, startedAtUtc ?? DateTime.UtcNow, ct);
            return new { sessionId = id };
        }, cancellationToken);
    }
}
