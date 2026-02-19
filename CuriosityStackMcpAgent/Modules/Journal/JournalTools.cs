using System.ComponentModel;
using CuriosityStack.Mcp.Core;
using CuriosityStack.Mcp.Core.Observability;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Journal;

[McpServerToolType]
public sealed class JournalTools
{
    private readonly IJournalService _journal;
    private readonly IToolExecutionRunner _runner;

    public JournalTools(IJournalService journal, IToolExecutionRunner runner)
    {
        _journal = journal;
        _runner = runner;
    }

    [Description("Add a journal entry.")]
    [McpServerTool(Name = "journal.add_entry")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Writes to Entries.", false)]
    public Task<string> AddEntryAsync(
        [Description("Entry title")] string title,
        [Description("Entry content")] string content,
        [Description("Comma-separated tags")] string? tags = null,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync("journal", "journal.add_entry", new(ScopeClassification.Write, ApprovalLevel.None, "Writes to Entries.", false), McpExecutionContext.Create(), async ct => new { entryId = await _journal.AddEntryAsync(title, content, tags, ct) }, cancellationToken);

    [Description("Log a decision with rationale.")]
    [McpServerTool(Name = "journal.log_decision")]
    [ToolPolicy(ScopeClassification.Write, ApprovalLevel.None, "Writes to Decisions.", false)]
    public Task<string> LogDecisionAsync(
        [Description("Decision statement")] string decisionText,
        [Description("Rationale")] string rationale,
        [Description("Expected outcome")] string? expectedOutcome = null,
        CancellationToken cancellationToken = default)
        => _runner.RunAsync("journal", "journal.log_decision", new(ScopeClassification.Write, ApprovalLevel.None, "Writes to Decisions.", false), McpExecutionContext.Create(), async ct => new { decisionId = await _journal.LogDecisionAsync(decisionText, rationale, expectedOutcome, ct) }, cancellationToken);

    [Description("Analyze recurring journal patterns.")]
    [McpServerTool(Name = "journal.pattern_analysis")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> PatternAnalysisAsync(CancellationToken cancellationToken = default)
        => _runner.RunAsync("journal", "journal.pattern_analysis", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _journal.PatternAnalysisAsync(ct), cancellationToken);

    [Description("Estimate goal alignment from recent entries.")]
    [McpServerTool(Name = "journal.goal_alignment")]
    [ToolPolicy(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true)]
    public Task<string> GoalAlignmentAsync(CancellationToken cancellationToken = default)
        => _runner.RunAsync("journal", "journal.goal_alignment", new(ScopeClassification.ReadOnly, ApprovalLevel.None, "No side effects.", true), McpExecutionContext.Create(), ct => _journal.GoalAlignmentAsync(ct), cancellationToken);
}
