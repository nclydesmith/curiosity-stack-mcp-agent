namespace CuriosityStack.Mcp.Configuration;

/// <summary>
/// Configuration settings for the Curiosity Stack MCP Agent.
/// Loaded from appsettings.json under "McpAgent" section.
/// </summary>
public sealed class McpAgentSettings
{
    public required string Name { get; set; } = "curiosity-mcp-agent";
    public required string Version { get; set; } = "0.1.0";
    
    /// <summary>
    /// MCP transport configuration
    /// </summary>
    public TransportSettings Transport { get; set; } = new();
    
    /// <summary>
    /// Xero API configuration
    /// </summary>
    public XeroSettings Xero { get; set; } = new();
    
    /// <summary>
    /// Git repository configuration
    /// </summary>
    public GitSettings Git { get; set; } = new();
    
    /// <summary>
    /// Governance and approval policies
    /// </summary>
    public GovernanceSettings Governance { get; set; } = new();

    /// <summary>
    /// Personal MCP storage and deployment mode settings.
    /// </summary>
    public PersonalMcpSettings Personal { get; set; } = new();
}

public sealed class PersonalMcpSettings
{
    /// <summary>
    /// Mode: local-personal or hosted-secure (future).
    /// </summary>
    public string Mode { get; set; } = "local-personal";

    /// <summary>
    /// Local SQLite file path.
    /// </summary>
    public string SqlitePath { get; set; } = "data/personal-mcp.db";
}

public sealed class TransportSettings
{
    /// <summary>
    /// Transport type: "stdio" or "http"
    /// </summary>
    public string Type { get; set; } = "stdio";
    
    /// <summary>
    /// HTTP server port (if Type = "http")
    /// </summary>
    public int? HttpPort { get; set; } = 3000;
}

public sealed class XeroSettings
{
    /// <summary>
    /// Xero tenant ID (organization)
    /// </summary>
    public string TenantId { get; set; } = "";
    
    /// <summary>
    /// OAuth 2.0 client ID
    /// </summary>
    public string ClientId { get; set; } = "";
    
    /// <summary>
    /// OAuth 2.0 client secret
    /// </summary>
    public string ClientSecret { get; set; } = "";
    
    /// <summary>
    /// OAuth 2.0 redirect URI
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:8080/callback";
    
    /// <summary>
    /// Xero API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.xero.com/api.xro/2.0";
    
    /// <summary>
    /// OAuth token endpoint
    /// </summary>
    public string TokenUrl { get; set; } = "https://identity.xero.com/connect/token";
}

public sealed class GitSettings
{
    /// <summary>
    /// Default local repository path
    /// </summary>
    public string DefaultRepoPath { get; set; } = ".";
    
    /// <summary>
    /// Git remote URL (optional, for clone operations)
    /// </summary>
    public string? RemoteUrl { get; set; }
    
    /// <summary>
    /// Protected branches that require approval before modification
    /// </summary>
    public string[] ProtectedBranches { get; set; } = ["main", "master", "production"];
}

public sealed class GovernanceSettings
{
    /// <summary>
    /// Enable approval gates for sensitive operations
    /// </summary>
    public bool EnableApprovalGates { get; set; } = true;
    
    /// <summary>
    /// Operations that require explicit approval
    /// </summary>
    public SensitiveOperation[] SensitiveOperations { get; set; } = 
    [
        new() { Domain = "finance", Operation = "create_invoice", Scope = OperationScope.Write },
        new() { Domain = "finance", Operation = "post_journal", Scope = OperationScope.Sensitive },
        new() { Domain = "finance", Operation = "modify_account", Scope = OperationScope.Sensitive },
        new() { Domain = "git", Operation = "merge_protected_branch", Scope = OperationScope.Sensitive },
        new() { Domain = "git", Operation = "force_push", Scope = OperationScope.Sensitive },
        new() { Domain = "git", Operation = "delete_branch", Scope = OperationScope.Sensitive },
    ];
    
    /// <summary>
    /// Approval timeout in seconds
    /// </summary>
    public int ApprovalTimeoutSeconds { get; set; } = 300;
}

public sealed class SensitiveOperation
{
    public required string Domain { get; set; }
    public required string Operation { get; set; }
    public required OperationScope Scope { get; set; }
    public string? Description { get; set; }
}

public enum OperationScope
{
    ReadOnly = 0,
    Write = 1,
    Sensitive = 2,
}
