using System.Text.Json.Serialization;

namespace CuriosityStack.Mcp.Models;

// ============================================================================
// XERO/FINANCE MODELS
// ============================================================================

/// <summary>
/// Xero Invoice Dto - for creating and retrieving invoices.
/// </summary>
public sealed record InvoiceDto
{
    [JsonPropertyName("invoiceID")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public required string InvoiceNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "DRAFT"; // DRAFT, SUBMITTED, AUTHORISED, PAID, VOIDED

    [JsonPropertyName("type")]
    public string Type { get; set; } = "ACCREC"; // ACCREC (invoice), ACCPAY (bill)

    [JsonPropertyName("contact")]
    public required ContactDto Contact { get; set; }

    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("lineItems")]
    public required LineItemDto[] LineItems { get; set; }

    [JsonPropertyName("total")]
    public decimal? Total { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed record ContactDto
{
    [JsonPropertyName("contactID")]
    public string? ContactId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("taxIDNumber")]
    public string? TaxId { get; set; }
}

public sealed record LineItemDto
{
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    [JsonPropertyName("unitAmount")]
    public required decimal UnitAmount { get; set; }

    [JsonPropertyName("accountCode")]
    public required string AccountCode { get; set; }

    [JsonPropertyName("taxType")]
    public string TaxType { get; set; } = "Tax on Sales";

    [JsonPropertyName("taxAmount")]
    public decimal? TaxAmount { get; set; }

    [JsonPropertyName("lineAmount")]
    public decimal? LineAmount { get; set; }
}

/// <summary>
/// Xero Account - chart of accounts.
/// </summary>
public sealed record AccountDto
{
    [JsonPropertyName("accountID")]
    public string? AccountId { get; set; }

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "ACTIVE";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("taxType")]
    public string? TaxType { get; set; }

    [JsonPropertyName("enablePayments")]
    public bool EnablePayments { get; set; }
}

/// <summary>
/// Xero Journal Entry - for posting financial transactions.
/// </summary>
public sealed record JournalDto
{
    [JsonPropertyName("journalID")]
    public string? JournalId { get; set; }

    [JsonPropertyName("journalDate")]
    public required DateTime JournalDate { get; set; }

    [JsonPropertyName("reference")]
    public required string Reference { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("lineItems")]
    public required JournalLineDto[] LineItems { get; set; }
}

public sealed record JournalLineDto
{
    [JsonPropertyName("lineAmount")]
    public required decimal Amount { get; set; }

    [JsonPropertyName("accountCode")]
    public required string AccountCode { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("lineItems")]
    public string? TaxType { get; set; }
}

/// <summary>
/// Xero Trial Balance Report.
/// </summary>
public sealed record TrialBalanceDto
{
    [JsonPropertyName("reportDate")]
    public DateTime ReportDate { get; set; }

    [JsonPropertyName("accounts")]
    public TrialBalanceAccountDto[] Accounts { get; set; } = [];
}

public sealed record TrialBalanceAccountDto
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("balance")]
    public decimal Balance { get; set; }
}

// ============================================================================
// GIT MODELS
// ============================================================================

/// <summary>
/// Git Repository metadata and status.
/// </summary>
public sealed record RepositoryInfoDto
{
    public required string Path { get; set; }
    public required string HeadBranch { get; set; }
    public required bool IsBare { get; set; }
    public string? RemoteUrl { get; set; }
    public CommitDto[] RecentCommits { get; set; } = [];
    public RepositoryStatusDto Status { get; set; } = new();
}

public sealed record RepositoryStatusDto
{
    public string[] UntrackedFiles { get; set; } = [];
    public string[] ModifiedFiles { get; set; } = [];
    public string[] StagedChanges { get; set; } = [];
    public bool IsClean { get; set; }
}

/// <summary>
/// Git Commit information.
/// </summary>
public sealed record CommitDto
{
    public required string Hash { get; set; }
    public required string Message { get; set; }
    public required string Author { get; set; }
    public required DateTime CommitDate { get; set; }
    public string? Tree { get; set; }
    public string[] Parents { get; set; } = [];
}

/// <summary>
/// Git Diff for a single file.
/// </summary>
public sealed record FileDiffDto
{
    public required string Path { get; set; }
    public required string Status { get; set; } // Added, Removed, Modified, Renamed
    public string? OldPath { get; set; } // For renames
    public int? Additions { get; set; }
    public int? Deletions { get; set; }
    public string? Patch { get; set; }
}

/// <summary>
/// Git Branch information.
/// </summary>
public sealed record BranchDto
{
    public required string Name { get; set; }
    public required string Tip { get; set; } // Commit hash at branch tip
    public bool IsRemote { get; set; }
    public DateTime? LastCommitDate { get; set; }
    public string? LastCommitMessage { get; set; }
}

/// <summary>
/// Pull Request / Merge Request.
/// </summary>
public sealed record MergeRequestDto
{
    public string? Id { get; set; }
    public required string SourceBranch { get; set; }
    public required string TargetBranch { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "OPEN"; // OPEN, MERGED, CLOSED, DRAFT
    public string? Author { get; set; }
    public DateTime? CreatedAt { get; set; }
    public CommitDto[] Commits { get; set; } = [];
}

// ============================================================================
// SHARED ERROR MODELS
// ============================================================================

/// <summary>
/// Standard error response for tool execution failures.
/// </summary>
public sealed record ToolErrorDto
{
    public required string ErrorCode { get; set; }
    public required string Message { get; set; }
    public string? Details { get; set; }
    public string? Stack { get; set; } // Only in debug mode
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Operation execution result wrapper.
/// </summary>
public sealed record OperationResult<T>
{
    public required bool Success { get; set; }
    public T? Data { get; set; }
    public ToolErrorDto? Error { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public long ElapsedMs { get; set; }
}
