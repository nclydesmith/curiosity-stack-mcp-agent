using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Observability;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Core.Governance;

[McpServerToolType]
public sealed class GovernanceTools
{
    private readonly IApprovalTokenService _tokens;
    private readonly IToolExecutionRunner _runner;

    public GovernanceTools(IApprovalTokenService tokens, IToolExecutionRunner runner)
    {
        _tokens = tokens;
        _runner = runner;
    }

    [Description("Request an explicit approval token for a write/sensitive tool.")]
    [McpServerTool(Name = "governance.request_approval")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects except ApprovalRecords write.", false)]
    public Task<string> RequestApprovalAsync(
        [Description("Domain name, e.g. finance/projects")] string domain,
        [Description("Tool name, e.g. finance.add_manual_entry")] string toolName,
        [Description("Scope: ReadOnly, Write, Sensitive")] ScopeClassification scope,
        [Description("TTL minutes")] int ttlMinutes = 10,
        CancellationToken cancellationToken = default)
    {
        var context = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "Creates ApprovalRecords.", false);

        return _runner.RunAsync("governance", "governance.request_approval", policy, context, async ct =>
        {
            var token = await _tokens.IssueTokenAsync(domain, toolName, scope, TimeSpan.FromMinutes(Math.Max(1, ttlMinutes)), ct);
            return new
            {
                approvalToken = token,
                domain,
                toolName,
                scope,
                expiresInMinutes = Math.Max(1, ttlMinutes),
            };
        }, cancellationToken);
    }
}
