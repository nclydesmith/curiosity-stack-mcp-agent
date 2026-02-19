using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Observability;
using CuriosityStack.Mcp.Finance;
using CuriosityStack.Mcp.Health;
using CuriosityStack.Mcp.Projects;
using ModelContextProtocol.Server;

namespace CuriosityStack.Agent.LifeOrchestrator;

[McpServerToolType]
public sealed class LifeOrchestratorTools
{
    private readonly IFinanceService _finance;
    private readonly IHealthService _health;
    private readonly IProjectsService _projects;
    private readonly IToolExecutionRunner _runner;

    public LifeOrchestratorTools(
        IFinanceService finance,
        IHealthService health,
        IProjectsService projects,
        IToolExecutionRunner runner)
    {
        _finance = finance;
        _health = health;
        _projects = projects;
        _runner = runner;
    }

    [Description("Aggregate daily life infrastructure status across finance, projects, and health.")]
    [McpServerTool(Name = "life.status")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> StatusAsync(
        [Description("Active tenant override")] string? activeTenant = null,
        [Description("Active project override")] string? activeProject = null,
        CancellationToken cancellationToken = default)
    {
        var context = McpExecutionContext.Create(activeTenant, activeProject);
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);

        return _runner.RunAsync("life", "life.status", policy, context, async ct =>
        {
            var cashPosition = await _finance.GetCashPositionAsync(ct);
            var deadlines = await _projects.GetDeadlinesAsync(14, ct);
            var recovery = await _health.GetRecoveryScoreAsync(ct);

            return new
            {
                generatedAtUtc = DateTime.UtcNow,
                activeTenant = context.ActiveTenant,
                activeProject = context.ActiveProject,
                summary = new
                {
                    cashPosition,
                    deadlines,
                    recovery,
                }
            };
        }, cancellationToken);
    }
}
