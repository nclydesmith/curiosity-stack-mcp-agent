using CuriosityStack.Mcp.Configuration;
using Microsoft.Extensions.Options;

namespace CuriosityStack.Mcp.Infrastructure.Governance;

/// <summary>
/// Represents an approval request pending user consent.
/// </summary>
public sealed record ApprovalRequest(
    string RequestId,
    string Domain,
    string ToolName,
    string Operation,
    OperationScope Scope,
    Dictionary<string, object> Parameters,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
}

/// <summary>
/// Represents an approval decision (approved/denied).
/// </summary>
public sealed record ApprovalDecision(
    string RequestId,
    bool Approved,
    string? ApprovalToken,
    string? Reason,
    DateTime DecidedAtUtc);

/// <summary>
/// Manages approval gates for sensitive operations.
/// Non-negotiable: Sensitive operations must receive explicit approval before execution.
/// </summary>
public interface IApprovalGateManager
{
    /// <summary>
    /// Check if an operation requires approval.
    /// </summary>
    bool RequiresApproval(string domain, string toolName, OperationScope scope);
    
    /// <summary>
    /// Create an approval request and waait for user decision.
    /// Returns approval token if approved, throws if denied or timeout.
    /// </summary>
    Task<string> RequestApprovalAsync(
        string domain,
        string operation,
        OperationScope scope,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate an approval token (idempotent).
    /// </summary>
    Task<bool> ValidateApprovalTokenAsync(string approvalToken);
}

/// <summary>
/// Defines governance policies for tool operations.
/// </summary>
public interface IGovernancePolicy
{
    /// <summary>
    /// Determine if a tool operation is allowed, requires approval, or is forbidden.
    /// </summary>
    OperationScope GetOperationScope(string domain, string toolName);
    
    /// <summary>
    /// Get the approval message to show the user.
    /// </summary>
    string GetApprovalPrompt(string domain, string operation, Dictionary<string, object> parameters);
}

/// <summary>
/// Default implementation of approval gate manager.
/// Stores pending requests in memory and requires external approval mechanism.
/// </summary>
public sealed class ApprovalGateManager : IApprovalGateManager
{
    private readonly IGovernancePolicy _policy;
    private readonly IOptions<McpAgentSettings> _settings;
    private readonly Dictionary<string, ApprovalRequest> _pendingApprovals = new();
    private readonly Dictionary<string, ApprovalDecision> _decisions = new();

    public ApprovalGateManager(
        IGovernancePolicy policy,
        IOptions<McpAgentSettings> settings)
    {
        _policy = policy;
        _settings = settings;
    }

    public bool RequiresApproval(string domain, string toolName, OperationScope scope)
    {
        if (!_settings.Value.Governance.EnableApprovalGates)
            return false;

        var operationScope = _policy.GetOperationScope(domain, toolName);
        return operationScope >= OperationScope.Write;
    }

    public async Task<string> RequestApprovalAsync(
        string domain,
        string operation,
        OperationScope scope,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Value.Governance.EnableApprovalGates)
        {
            // If governance disabled, generate a mock token
            return $"token_{Guid.NewGuid():N}";
        }

        var requestId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddSeconds(
            _settings.Value.Governance.ApprovalTimeoutSeconds);

        var request = new ApprovalRequest(
            requestId,
            domain,
            operation,
            operation,
            scope,
            parameters,
            DateTime.UtcNow,
            expiresAt);

        _pendingApprovals[requestId] = request;

        // Output approval prompt to stderr (visible to user running the agent)
        var prompt = _policy.GetApprovalPrompt(domain, operation, parameters);
        Console.Error.WriteLine($"\n[APPROVAL REQUIRED]");
        Console.Error.WriteLine($"Request ID: {requestId}");
        Console.Error.WriteLine($"Domain: {domain}");
        Console.Error.WriteLine($"Operation: {operation}");
        Console.Error.WriteLine($"Scope: {scope}");
        Console.Error.WriteLine($"Expires: {expiresAt:O}");
        Console.Error.WriteLine($"\nPrompt:\n{prompt}\n");

        // Wait for external approval mechanism or timeout
        // In a real implementation, this would integrate with:
        // - Claude API sampling (request user input via model)
        // - CLI prompt (if running interactively)
        // - HTTP webhook (if running as service)
        
        var timeoutMs = _settings.Value.Governance.ApprovalTimeoutSeconds * 1000;
        var watch = System.Diagnostics.Stopwatch.StartNew();

        while (watch.ElapsedMilliseconds < timeoutMs)
        {
            if (_decisions.TryGetValue(requestId, out var decision))
            {
                _pendingApprovals.Remove(requestId);
                
                if (!decision.Approved)
                {
                    throw new OperationCanceledException(
                        $"Operation denied by user. Reason: {decision.Reason}");
                }

                return decision.ApprovalToken 
                    ?? throw new InvalidOperationException("No approval token provided");
            }

            await Task.Delay(100, cancellationToken);
        }

        _pendingApprovals.Remove(requestId);
        throw new TimeoutException(
            $"Approval request {requestId} expired after {_settings.Value.Governance.ApprovalTimeoutSeconds} seconds");
    }

    public Task<bool> ValidateApprovalTokenAsync(string approvalToken)
    {
        // In a real system, validate against a secure store or cryptographic signature
        // For now, simple check if token format is valid
        var isValid = approvalToken.StartsWith("token_") && approvalToken.Length > 6;
        return Task.FromResult(isValid);
    }

    /// <summary>
    /// Method to be called by approval mechanism (CLI, HTTP, sampling) to decide a pending request.
    /// </summary>
    public void SubmitApprovalDecision(string requestId, bool approved, string? reason = null)
    {
        if (!_pendingApprovals.ContainsKey(requestId))
            throw new InvalidOperationException($"No pending approval request: {requestId}");

        var token = approved ? $"token_{Guid.NewGuid():N}" : null;
        _decisions[requestId] = new ApprovalDecision(
            requestId,
            approved,
            token,
            reason,
            DateTime.UtcNow);
    }
}

/// <summary>
/// Default governance policy implementation.
/// Loads scope rules from configuration.
/// </summary>
public sealed class DefaultGovernancePolicy : IGovernancePolicy
{
    private readonly IOptions<McpAgentSettings> _settings;

    public DefaultGovernancePolicy(IOptions<McpAgentSettings> settings)
    {
        _settings = settings;
    }

    public OperationScope GetOperationScope(string domain, string toolName)
    {
        // Check if this tool is in the sensitive operations list
        var sensitiveOps = _settings.Value.Governance.SensitiveOperations;
        var match = sensitiveOps.FirstOrDefault(op =>
            op.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase) &&
            op.Operation.Equals(toolName, StringComparison.OrdinalIgnoreCase));

        return match?.Scope ?? OperationScope.ReadOnly;
    }

    public string GetApprovalPrompt(string domain, string operation, Dictionary<string, object> parameters)
    {
        var paramStr = string.Join(", ", parameters.Select(kv => $"{kv.Key}={kv.Value}"));
        
        return $@"GOVERNANCE GATE: Sensitive Operation Requires Approval

Domain: {domain}
Operation: {operation}
Parameters: {paramStr}

This operation modifies system state or has high business impact.
It requires explicit user approval before execution.

To approve, call: agent.ApproveRequest(requestId, approved=true)
To deny, call: agent.ApproveRequest(requestId, approved=false, reason='...')";
    }
}
