using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Governance;
using CuriosityStack.Mcp.Core.Observability;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Projects;

[McpServerToolType]
public sealed class ProjectsTools
{
    private readonly IProjectsService _projects;
    private readonly IToolPolicyEnforcer _policy;
    private readonly IToolExecutionRunner _runner;

    public ProjectsTools(IProjectsService projects, IToolPolicyEnforcer policy, IToolExecutionRunner runner)
    {
        _projects = projects;
        _policy = policy;
        _runner = runner;
    }

    [Description("List active projects.")]
    [McpServerTool(Name = "projects.active")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> ActiveAsync(CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.active", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _projects.GetActiveAsync(ct), cancellationToken);

    [Description("List upcoming project deadlines.")]
    [McpServerTool(Name = "projects.deadlines")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> DeadlinesAsync([Description("Window days")] int days = 14, CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.deadlines", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _projects.GetDeadlinesAsync(days, ct), cancellationToken);

    [Description("Compute project velocity summary.")]
    [McpServerTool(Name = "projects.velocity")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> VelocityAsync([Description("Window days")] int days = 30, CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.velocity", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _projects.GetVelocityAsync(days, ct), cancellationToken);

    [Description("List open risk flags.")]
    [McpServerTool(Name = "projects.risk_flags")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> RiskFlagsAsync(CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.risk_flags", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _projects.GetRiskFlagsAsync(ct), cancellationToken);

    [Description("Create a new project.")]
    [McpServerTool(Name = "projects.create")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Writes to ProjectRegistry.", false)]
    public Task<string> CreateAsync(
        [Description("Project name")] string name,
        [Description("Project owner")] string owner,
        [Description("Optional due date")] DateTime? dueDateUtc = null,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.create", new(ScopeClassification.Write, ApprovalLevel.None, "Writes to ProjectRegistry.", false), McpExecutionContext.Create(), async ct => new { projectId = await _projects.CreateAsync(name, owner, dueDateUtc, ct) }, cancellationToken);

    [Description("Update an existing project status.")]
    [McpServerTool(Name = "projects.update_status")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Updates ProjectRegistry.Status.", false)]
    public Task<string> UpdateStatusAsync(
        [Description("Project id")] string projectId,
        [Description("New status") ] string status,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.update_status", new(ScopeClassification.Write, ApprovalLevel.None, "Updates ProjectRegistry.Status.", false), McpExecutionContext.Create(), async ct => new { updated = await _projects.UpdateStatusAsync(projectId, status, ct) }, cancellationToken);

    [Description("Archive a project (sensitive, approval required).")]
    [McpServerTool(Name = "projects.archive")]
    [ToolPolicy(ScopeClassification.Sensitive, ApprovalLevel.ExplicitToken, "Marks ProjectRegistry row as archived.", false)]
    public Task<string> ArchiveAsync(
        [Description("Project id")] string projectId,
        [Description("Approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync("projects", "projects.archive", new(ScopeClassification.Sensitive, ApprovalLevel.ExplicitToken, "Marks ProjectRegistry row as archived.", false), McpExecutionContext.Create(), async ct =>
        {
            var policy = new ToolPolicyDescriptor(ScopeClassification.Sensitive, ApprovalLevel.ExplicitToken, "Marks ProjectRegistry row as archived.", false);
            await _policy.EnforceAsync("projects", "projects.archive", policy, approvalToken, ct);
            return new { archived = await _projects.ArchiveAsync(projectId, ct) };
        }, cancellationToken);
}
