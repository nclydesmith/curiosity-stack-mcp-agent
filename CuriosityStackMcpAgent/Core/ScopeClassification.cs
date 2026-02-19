namespace CuriosityStack.Mcp.Core;

public enum ScopeClassification
{
    ReadOnly = 0,
    Write = 1,
    Sensitive = 2,
}

public enum ApprovalLevel
{
    None = 0,
    ExplicitToken = 1,
}
