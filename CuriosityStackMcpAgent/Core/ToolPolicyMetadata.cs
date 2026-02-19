namespace CuriosityStack.Mcp.Core;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ToolPolicyAttribute : Attribute
{
    public ToolPolicyAttribute(
        ScopeClassification scope,
        ApprovalLevel requiredApproval,
        string expectedSideEffects,
        bool isIdempotent)
    {
        Scope = scope;
        RequiredApproval = requiredApproval;
        ExpectedSideEffects = expectedSideEffects;
        IsIdempotent = isIdempotent;
    }

    public ScopeClassification Scope { get; }
    public ApprovalLevel RequiredApproval { get; }
    public string ExpectedSideEffects { get; }
    public bool IsIdempotent { get; }
}

public sealed record ToolPolicyDescriptor(
    ScopeClassification Scope,
    ApprovalLevel RequiredApproval,
    string ExpectedSideEffects,
    bool IsIdempotent);
