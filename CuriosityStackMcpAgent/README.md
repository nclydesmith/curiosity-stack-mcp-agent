# Curiosity Stack MCP Agent (.NET/C#)

A protocol-governed, production-ready Model Context Protocol (MCP) server implementing Xero (Finance) and Git (Version Control) domain tools with built-in governance and approval gate infrastructure.

## Architecture Overview

### Project Structure

```
CuriosityStackMcpAgent/
├── Program.cs                          # Entry point & DI configuration
├── appsettings.json                    # Configuration (Xero, Git, Governance)
│
├── Tools/                              # MCP Tool implementations
│   ├── Finance/
│   │   ├── IXeroApiClient.cs           # Xero API contract (ReadOnly, Write, Sensitive)
│   │   ├── XeroApiClient.cs            # REST implementation with OAuth support
│   │   └── XeroTools.cs                # MCP tools (xero_*)
│   │
│   └── Git/
│       ├── IGitRepository.cs           # Git operations contract
│       ├── LibGit2SharpRepository.cs  # LibGit2Sharp implementation
│       └── GitTools.cs                 # MCP tools (git_*)
│
├── Models/
│   └── XeroGitModels.cs               # DTOs for Xero, Git, shared
│
├── Configuration/
│   └── McpAgentSettings.cs            # Configuration classes & enums
│
├── Infrastructure/
│   ├── Governance/
│   │   └── ApprovalGateManager.cs     # Approval gate implementation
│   ├── Logging/
│   │   └── (Stderr logging configuration)
│   └── ErrorHandling/
│       └── (Error response normalization)
│
└── Tests/
    └── (Unit & integration tests)
```

---

## Core Design Principles

### 1. **Protocol Governance (Non-Negotiable)**
All external actions flow through MCP tools only. No fabricated data or out-of-band operations.

**Tool Scopes:**
- **ReadOnly**: Always available, safe queries (trial balance, commit log, contact list)
- **Write**: Non-destructive state changes (create invoice, stage files). May require approval.
- **Sensitive**: High-impact or destructive (post journal, merge branch, reset HEAD). ALWAYS requires approval.

### 2. **Approval Gates**
Sensitive operations must receive explicit approval before execution.

**Flow:**
1. Agent calls `xero_request_approval` or `git_request_approval`
2. ApprovalGateManager creates approval request, outputs prompt to stderr
3. User/external system reviews request and calls `SubmitApprovalDecision(requestId, approved)`
4. Agent receives approval token and uses it with the operation tool
5. Tool validates token, executes, or fails safely

**Configurable:** `appsettings.json` defines which operations require approval.

### 3. **Strict Logging to Stderr**
STDIO transport requires all logs to stderr to preserve stdout for JSON-RPC protocol.

```csharp
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
```

### 4. **Error Handling**
All tools return JSON error responses (never throw). Errors include:
- ErrorCode (e.g., `APPROVAL_REQUIRED`, `INVOICE_NOT_FOUND`)
- Message (user-friendly description)
- Context (operation details)

---

## Finance Tools (Xero)

### ReadOnly Tools
- **xero_get_trial_balance** - Trial balance report
- **xero_list_accounts** - Chart of accounts  
- **xero_get_account** - Account details by code
- **xero_list_invoices** - Invoices for a contact
- **xero_get_invoice** - Invoice details
- **xero_list_contacts** - All contacts
- **xero_validate_connection** - Validate API connection

### Write Tools (Require Approval)
- **xero_create_invoice** - Create new invoice
- **xero_authorize_invoice** - Authorize/finalize invoice

### Sensitive Tools (Require Governance Approval)
- **xero_post_journal** - Post manual journal entry (modifies ledger directly)
- **xero_update_account** - Modify account settings

### Support Tools
- **xero_request_approval** - Request approval for an operation

---

## Git Tools (Version Control)

### ReadOnly Tools
- **git_get_repo_info** - Current branch, status, recent commits
- **git_get_commit_log** - Commit history
- **git_get_commit** - Specific commit details
- **git_list_branches** - All branches (local/remote)
- **git_get_current_branch** - Current branch name
- **git_get_status** - Staged/unstaged/untracked files
- **git_get_diff** - Diff between commits/branches

### Write Tools
- **git_stage_files** - Stage files for commit
- **git_commit** - Create commit
- **git_checkout** - Switch branch

### Sensitive Tools (Require Approval)
- **git_merge** - Merge branch (protected branches blocked without approval)
- **git_delete_branch** - Delete branch (protected branches cannot be deleted)
- **git_reset** - Reset to commit (destructive)

### Support Tools
- **git_request_approval** - Request approval for sensitive operation

---

## Configuration

### appsettings.json Structure

```json
{
  "McpAgent": {
    "Transport": {
      "Type": "stdio|http"
    },
    "Xero": {
      "TenantId": "...",
      "ClientId": "...",
      "ClientSecret": "...",
      "ApiBaseUrl": "https://api.xero.com/api.xro/2.0"
    },
    "Git": {
      "DefaultRepoPath": ".",
      "ProtectedBranches": ["main", "master", "production"]
    },
    "Governance": {
      "EnableApprovalGates": true,
      "ApprovalTimeoutSeconds": 300,
      "SensitiveOperations": [...]
    }
  }
}
```

### Xero OAuth Setup

Required environment variables or `appsettings.json`:
```
XERO_TENANT_ID=<Your Organization ID>
XERO_CLIENT_ID=<OAuth Client ID>
XERO_CLIENT_SECRET=<OAuth Client Secret>
```

**Note:** Current implementation uses mock token. Integrate with Xero's OAuth 2.0 flow:
```csharp
// In XeroApiClient.EnsureTokenAsync()
// 1. Get authorization code from user
// 2. POST to _settings.TokenUrl with code
// 3. Extract access_token and refresh_token
// 4. Store securely for future use
```

---

## Running the Server

### Prerequisites
- .NET 8.0 SDK
- LibGit2Sharp (auto-installed via NuGet)
- For Xero: OAuth credentials from Xero developer portal

### Build
```powershell
cd CuriosityStackMcpAgent
dotnet build
```

### Run (STDIO Transport)
```powershell
dotnet run
```

The agent listens on stdin/stdout for JSON-RPC protocol messages. All logging goes to stderr.

### Run (HTTP Transport)
```powershell
dotnet run -- --transport http --port 3000
```

---

## Tool Invocation Examples

### Example 1: Get Trial Balance (ReadOnly)
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "xero_get_trial_balance",
    "arguments": {}
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "success": true,
    "data": {
      "reportDate": "2026-02-18",
      "accounts": [...]
    }
  }
}
```

### Example 2: Create Invoice (Write - Requires Approval)

**Step 1: Request Approval**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "xero_request_approval",
    "arguments": {
      "operation": "xero_create_invoice",
      "parametersJson": "{\"invoiceNumber\": \"INV-001\"}"
    }
  }
}
```

**Response (with approval token):**
```json
{
  "success": true,
  "approvalToken": "token_abc123...",
  "message": "Approval granted"
}
```

**Step 2: Execute with Token**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "xero_create_invoice",
    "arguments": {
      "invoiceJson": "{\"invoiceNumber\": \"INV-001\", ...}",
      "approvalToken": "token_abc123..."
    }
  }
}
```

### Example 3: Merge Branch (Sensitive - Requires Governance Approval)

**Step 1: Request Approval**
```json
{
  "method": "tools/call",
  "params": {
    "name": "git_request_approval",
    "arguments": {
      "operation": "git_merge",
      "parametersJson": "{\"branchName\": \"feature/xyz\", \"targetBranch\": \"main\"}"
    }
  }
}
```

**Stderr Output:**
```
[APPROVAL REQUIRED]
Request ID: abc123-def456
Domain: git
Operation: git_merge
Scope: Sensitive
Expires: 2026-02-18T10:30:45Z

Prompt:
GOVERNANCE GATE: Sensitive Operation Requires Approval
Domain: git
Operation: git_merge
...
```

**Step 2: User/System Approves (external mechanism)**
```csharp
// In your approval handler:
approvalGateManager.SubmitApprovalDecision("abc123-def456", approved: true);
```

**Step 3: Execute with Token**
```json
{
  "method": "tools/call",
  "params": {
    "name": "git_merge",
    "arguments": {
      "branchName": "feature/xyz",
      "approvalToken": "token_xyz..."
    }
  }
}
```

---

## Governance & Compliance

### Non-Negotiables
1. **Never fabricate tool results** - If an API call fails, return clear error
2. **Approval gates are mandatory** - Sensitive operations blocked without token
3. **No privilege escalation** - Tools operate within scope defined in configuration
4. **Strict error boundaries** - No secrets or credentials in error messages
5. **Audit logging** - All write/sensitive operations logged to stderr with timestamp

### Audit Trail Example
```
[10:15:23] [WARN] [XeroTools] CreateInvoice called - WRITE operation
[10:15:24] [WARN] [XeroTools] Creating invoice INV-001
[10:15:25] [WARN] [XeroTools] Invoice created 51234567-89ab-cdef-0123-456789abcdef
```

---

## Testing & Development

### Unit Tests (Xunit + Moq)
```csharp
[Fact]
public async Task CreateInvoice_WithoutApproval_ReturnError()
{
    // Arrange
    var mockClient = new Mock<IXeroApiClient>();
    var mockGate = new Mock<IApprovalGateManager>();
    
    // Act
    var result = await _tools.CreateInvoiceAsync(invoiceJson, null);
    
    // Assert
    Assert.Contains("APPROVAL_REQUIRED", result);
}
```

### Integration Tests
Test full flow: tool → API client → error handling:
```csharp
[Fact]
public async Task GetTrialBalance_ReturnsFormattedResponse()
{
    // Use real HttpClient with mock server
    // Verify stderr logging
    // Verify JSON-RPC response format
}
```

---

## Extension Points

### Adding a New Domain

1. **Create domain interface:**
   ```csharp
   public interface IMyDomainClient
   {
       Task<DataDto> GetDataAsync(...);
   }
   ```

2. **Implement service:**
   ```csharp
   public sealed class MyDomainClient : IMyDomainClient { ... }
   ```

3. **Create tools class:**
   ```csharp
   public sealed class MyDomainTools { ... }
   ```

4. **Register in Program.cs:**
   ```csharp
   builder.Services.AddHttpClient<IMyDomainClient, MyDomainClient>();
   builder.Services.AddSingleton<MyDomainTools>();
   ```

5. **Update governance policies:**
   ```csharp
   // Add operations to SensitiveOperations array in appsettings.json
   ```

---

## Known Limitations & TODOs

- [ ] Xero OAuth token refresh logic (currently uses mock token)
- [ ] HTTP/SSE transport (stub only, STDIO working)
- [ ] Database persistence for approval tokens
- [ ] Approval mechanism integration (CLI prompt, HTTP webhook, Claude sampling)
- [ ] Comprehensive error catalog and retry logic
- [ ] Performance testing with 50+ tools
- [ ] Multi-tenant Xero support

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                     MCP Client (Claude, etc)            │
└────────────────────┬────────────────────────────────────┘
                     │ JSON-RPC
                     │ (stdio/HTTP)
                     ▼
┌─────────────────────────────────────────────────────────┐
│           MCP Protocol Handler (Framework)              │
├─────────────────────────────────────────────────────────┤
│  Tools Discovery │ Resource Discovery │ Prompt Registry │
└────────────────┬──────────────────────────────────────┬─┘
                 │                                      │
        ┌────────▼────────────┐            ┌──────────▼──────┐
        │   Finance Tools     │            │   Git Tools     │
        ├─────────────────────┤            ├─────────────────┤
        │ • xero_*_read       │            │ • git_*_read    │
        │ • xero_*_write      │            │ • git_*_write   │
        │ • xero_*_sensitive  │            │ • git_*_sensitive
        └────────┬────────────┘            └────────┬────────┘
                 │                                  │
        ┌────────▼────────────┐            ┌──────────▼──────┐
        │  Xero API Client    │            │ Git Repository  │
        │  • OAuth handling   │            │ • LibGit2Sharp  │
        │  • REST calls       │            │ • Local repos   │
        └────────┬────────────┘            └────────┬────────┘
                 │                                  │
        ┌────────▼──────────┐              ┌─────────▼────┐
        │  Xero REST API    │              │  Local Git   │
        │ api.xero.com      │              │  Repositories
        └───────────────────┘              └──────────────┘

                    ▲
                    │
        ┌───────────┴──────────┐
        │  Approval Gate Mgr   │
        │ (validates tokens)   │
        └──────────┬───────────┘
                   │ stderr
        ┌──────────▼──────────┐
        │  User/External      │
        │  Approval Handler   │
        └─────────────────────┘
```

---

## References

- **MCP Specification:** https://modelcontextprotocol.io/specification
- **MCP C# SDK:** https://github.com/modelcontextprotocol/csharp-sdk
- **Xero API Docs:** https://developer.xero.com/documentation
- **LibGit2Sharp:** https://github.com/libgit2/libgit2sharp

## License

Proprietary - Curiosity Stack
