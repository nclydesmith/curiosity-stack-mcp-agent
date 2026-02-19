namespace CuriosityStack.Mcp.Core.Governance;

public interface IToolPolicyEnforcer
{
    Task EnforceAsync(
        string domain,
        string toolName,
        ToolPolicyDescriptor policy,
        string? approvalToken,
        CancellationToken cancellationToken = default);
}

public sealed class ToolPolicyEnforcer : IToolPolicyEnforcer
{
    private readonly IApprovalTokenService _approvalTokens;

    public ToolPolicyEnforcer(IApprovalTokenService approvalTokens)
    {
        _approvalTokens = approvalTokens;
    }

    public async Task EnforceAsync(
        string domain,
        string toolName,
        ToolPolicyDescriptor policy,
        string? approvalToken,
        CancellationToken cancellationToken = default)
    {
        if (policy.RequiredApproval == ApprovalLevel.None)
        {
            return;
        }

        var valid = await _approvalTokens.ValidateTokenAsync(
            approvalToken ?? string.Empty,
            domain,
            toolName,
            policy.Scope,
            cancellationToken);

        if (!valid)
        {
            throw new InvalidOperationException("APPROVAL_REQUIRED: valid approval token is required for this operation.");
        }
    }
}
