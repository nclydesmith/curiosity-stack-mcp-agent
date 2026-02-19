using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Governance;
using CuriosityStack.Mcp.Core.Observability;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Finance;

[McpServerToolType]
public sealed class FinanceTools
{
    private readonly IFinanceService _finance;
    private readonly IToolPolicyEnforcer _policy;
    private readonly IToolExecutionRunner _runner;

    public FinanceTools(IFinanceService finance, IToolPolicyEnforcer policy, IToolExecutionRunner runner)
    {
        _finance = finance;
        _policy = policy;
        _runner = runner;
    }

    [Description("Get current cash position.")]
    [McpServerTool(Name = "finance.cash_position")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> CashPositionAsync(CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("finance", "finance.cash_position", policy, ctx, async ct => new
        {
            cashPosition = await _finance.GetCashPositionAsync(ct),
            currency = "USD",
        }, cancellationToken);
    }

    [Description("Get total net worth from local ledger accounts and positions.")]
    [McpServerTool(Name = "finance.net_worth")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> NetWorthAsync(CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("finance", "finance.net_worth", policy, ctx, async ct => new
        {
            netWorth = await _finance.GetNetWorthAsync(ct),
            currency = "USD",
        }, cancellationToken);
    }

    [Description("Get aggregate margin exposure.")]
    [McpServerTool(Name = "finance.margin_exposure")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> MarginExposureAsync(CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("finance", "finance.margin_exposure", policy, ctx, async ct => new
        {
            marginExposure = await _finance.GetMarginExposureAsync(ct),
            currency = "USD",
        }, cancellationToken);
    }

    [Description("Get monthly burn estimate from cash flow snapshots.")]
    [McpServerTool(Name = "finance.monthly_burn")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> MonthlyBurnAsync(CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true);
        return _runner.RunAsync("finance", "finance.monthly_burn", policy, ctx, async ct => new
        {
            monthlyBurn = await _finance.GetMonthlyBurnAsync(ct),
            currency = "USD",
        }, cancellationToken);
    }

    [Description("Add a manual finance ledger entry (approval required).")]
    [McpServerTool(Name = "finance.add_manual_entry")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.ExplicitToken, "Writes to FinanceManualEntries.", false)]
    public async Task<string> AddManualEntryAsync(
        [Description("Entry category")] string category,
        [Description("Signed amount")] decimal amount,
        [Description("Optional notes")] string? notes = null,
        [Description("Approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        var ctx = McpExecutionContext.Create();
        var policy = new ToolPolicyDescriptor(ScopeClassification.Write, ApprovalLevel.ExplicitToken, "Writes to FinanceManualEntries.", false);

        return await _runner.RunAsync("finance", "finance.add_manual_entry", policy, ctx, async ct =>
        {
            await _policy.EnforceAsync("finance", "finance.add_manual_entry", policy, approvalToken, ct);
            var id = await _finance.AddManualEntryAsync(category, amount, notes, ct);
            return new { entryId = id };
        }, cancellationToken);
    }
}
