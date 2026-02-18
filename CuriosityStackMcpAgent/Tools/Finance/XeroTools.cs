using CuriosityStack.Mcp.Infrastructure.Governance;
using CuriosityStack.Mcp.Models;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace CuriosityStack.Mcp.Tools.Finance;

/// <summary>
/// Finance domain tools for Xero integration.
/// Exposes invoice, account, and reporting operations via MCP protocol.
/// 
/// Tools are categorized by scope:
/// - ReadOnly: Always available, safe to expose
/// - Write: Require approval before execution
/// - Sensitive: Require governance gate approval
/// </summary>
[McpServerToolType]
public sealed class XeroTools
{
    private readonly IXeroApiClient _xeroClient;
    private readonly IApprovalGateManager _approvalGate;
    private readonly ILogger<XeroTools> _logger;

    public XeroTools(
        IXeroApiClient xeroClient,
        IApprovalGateManager approvalGate,
        ILogger<XeroTools> logger)
    {
        _xeroClient = xeroClient;
        _approvalGate = approvalGate;
        _logger = logger;
    }

    // ======================================================================
    // READ-ONLY OPERATIONS (Scope: ReadOnly)
    // ======================================================================

    /// <summary>
    /// Get trial balance (summary of all accounts).
    /// Tool: xero_get_trial_balance
    /// Scope: ReadOnly - always available
    /// </summary>
    [Description("Retrieve trial balance report showing all accounts and balances")]
    [McpServerTool(Name = "xero_get_trial_balance")]
    public async Task<string> GetTrialBalanceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[XeroTools] GetTrialBalance called");
            
            var result = await _xeroClient.GetTrialBalanceAsync(cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = result,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] GetTrialBalance failed");
            return ErrorResponse("GET_TRIAL_BALANCE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// List all accounts in chart of accounts.
    /// Tool: xero_list_accounts
    /// Scope: ReadOnly
    /// </summary>
    [Description("List all accounts in the chart of accounts")]
    [McpServerTool(Name = "xero_list_accounts")]
    public async Task<string> ListAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[XeroTools] ListAccounts called");
            
            var accounts = await _xeroClient.GetAccountsAsync(cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = accounts.Length,
                data = accounts,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] ListAccounts failed");
            return ErrorResponse("LIST_ACCOUNTS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get details of a specific account.
    /// Tool: xero_get_account
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get details of a specific account by code")]
    [McpServerTool(Name = "xero_get_account")]
    public async Task<string> GetAccountAsync(
        [Description("Account code (e.g., '200', '400')")] string code,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return ErrorResponse("INVALID_CODE", "Account code is required");
            
            _logger.LogInformation("[XeroTools] GetAccount called for code {Code}", code);
            
            var account = await _xeroClient.GetAccountByCodeAsync(code, cancellationToken);
            
            if (account == null)
                return ErrorResponse("ACCOUNT_NOT_FOUND", $"Account {code} not found");
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = account,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] GetAccount failed for code {Code}", code);
            return ErrorResponse("GET_ACCOUNT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// List invoices for a contact.
    /// Tool: xero_list_invoices
    /// Scope: ReadOnly
    /// </summary>
    [Description("List invoices for a specific contact")]
    [McpServerTool(Name = "xero_list_invoices")]
    public async Task<string> ListInvoicesAsync(
        [Description("Contact ID (UUID)")] string contactId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contactId))
                return ErrorResponse("INVALID_CONTACT_ID", "Contact ID is required");
            
            _logger.LogInformation("[XeroTools] ListInvoices called for contact {ContactId}", contactId);
            
            var invoices = await _xeroClient.GetInvoicesByContactAsync(contactId, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = invoices.Length,
                data = invoices,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] ListInvoices failed for contact {ContactId}", contactId);
            return ErrorResponse("LIST_INVOICES_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Get invoice details.
    /// Tool: xero_get_invoice
    /// Scope: ReadOnly
    /// </summary>
    [Description("Get details of a specific invoice")]
    [McpServerTool(Name = "xero_get_invoice")]
    public async Task<string> GetInvoiceAsync(
        [Description("Invoice ID (UUID)")] string invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
                return ErrorResponse("INVALID_INVOICE_ID", "Invoice ID is required");
            
            _logger.LogInformation("[XeroTools] GetInvoice called for {InvoiceId}", invoiceId);
            
            var invoice = await _xeroClient.GetInvoiceAsync(invoiceId, cancellationToken);
            
            if (invoice == null)
                return ErrorResponse("INVOICE_NOT_FOUND", $"Invoice {invoiceId} not found");
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = invoice,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] GetInvoice failed for {InvoiceId}", invoiceId);
            return ErrorResponse("GET_INVOICE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// List all contacts.
    /// Tool: xero_list_contacts
    /// Scope: ReadOnly
    /// </summary>
    [Description("List all contacts in Xero")]
    [McpServerTool(Name = "xero_list_contacts")]
    public async Task<string> ListContactsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[XeroTools] ListContacts called");
            
            var contacts = await _xeroClient.GetContactsAsync(cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                count = contacts.Length,
                data = contacts,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] ListContacts failed");
            return ErrorResponse("LIST_CONTACTS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Validate Xero connection and get active tenant.
    /// Tool: xero_validate_connection
    /// Scope: ReadOnly
    /// </summary>
    [Description("Validate Xero connection and get active tenant ID")]
    [McpServerTool(Name = "xero_validate_connection")]
    public async Task<string> ValidateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[XeroTools] ValidateConnection called");
            
            var tenantId = await _xeroClient.GetActiveTenantAsync(cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                tenantId = tenantId,
                connected = true,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] ValidateConnection failed");
            return JsonSerializer.Serialize(new
            {
                success = false,
                connected = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    // ======================================================================
    // WRITE OPERATIONS (Scope: Write, Require Approval)
    // ======================================================================

    /// <summary>
    /// Create a new invoice in Xero.
    /// Tool: xero_create_invoice
    /// Scope: Write - Requires approval before execution
    /// 
    /// Expects JSON invoice object with:
    /// - invoiceNumber: Unique invoice number
    /// - contact: { name, emailAddress }
    /// - lineItems: Array of { description, quantity, unitAmount, accountCode }
    /// - dueDate: ISO 8601 date
    /// </summary>
    [Description("Create a new invoice in Xero (requires approval)")]
    [McpServerTool(Name = "xero_create_invoice")]
    public async Task<string> CreateInvoiceAsync(
        [Description("Invoice JSON object")] string invoiceJson,
        [Description("Optional approval token from governance gate")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("[XeroTools] CreateInvoice called - WRITE operation");
            
            // Check if approval is required and validate
            if (!await ValidateApprovalAsync("finance", "xero_create_invoice", approvalToken, cancellationToken))
            {
                return ErrorResponse("APPROVAL_REQUIRED", 
                    "This operation requires approval. Request approval using: xero_request_approval");
            }
            
            // Parse invoice JSON
            var invoice = JsonSerializer.Deserialize<InvoiceDto>(invoiceJson);
            if (invoice == null)
                return ErrorResponse("INVALID_INVOICE_JSON", "Failed to parse invoice JSON");
            
            _logger.LogWarning("[XeroTools] Creating invoice {InvoiceNumber}", invoice.InvoiceNumber);
            
            var result = await _xeroClient.CreateInvoiceAsync(invoice, cancellationToken);
            
            _logger.LogWarning("[XeroTools] Invoice created {InvoiceId}", result.InvoiceId);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = result,
                message = "Invoice created successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] CreateInvoice failed");
            return ErrorResponse("CREATE_INVOICE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Authorize an invoice (finalize it).
    /// Tool: xero_authorize_invoice
    /// Scope: Write
    /// </summary>
    [Description("Authorize an invoice to finalize it (requires approval)")]
    [McpServerTool(Name = "xero_authorize_invoice")]
    public async Task<string> AuthorizeInvoiceAsync(
        [Description("Invoice ID")] string invoiceId,
        [Description("Optional approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
                return ErrorResponse("INVALID_INVOICE_ID", "Invoice ID is required");
            
            _logger.LogWarning("[XeroTools] AuthorizeInvoice called - WRITE operation");
            
            if (!await ValidateApprovalAsync("finance", "xero_authorize_invoice", approvalToken, cancellationToken))
                return ErrorResponse("APPROVAL_REQUIRED", "This operation requires approval");
            
            var result = await _xeroClient.AuthoriseInvoiceAsync(invoiceId, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = result,
                message = "Invoice authorized successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] AuthorizeInvoice failed");
            return ErrorResponse("AUTHORIZE_INVOICE_FAILED", ex.Message);
        }
    }

    // ======================================================================
    // SENSITIVE OPERATIONS (Scope: Sensitive, Require Governance Approval)
    // ======================================================================

    /// <summary>
    /// Post a journal entry (direct ledger modification).
    /// Tool: xero_post_journal
    /// Scope: Sensitive - Requires governance gate approval
    /// </summary>
    [Description("Post a journal entry to modify accounts directly (SENSITIVE - requires approval)")]
    [McpServerTool(Name = "xero_post_journal")]
    public async Task<string> PostJournalAsync(
        [Description("Journal entry JSON with lineItems")] string journalJson,
        [Description("Governance approval token")] string? approvalToken = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogError("[XeroTools] PostJournal called - SENSITIVE operation");
            
            if (!await ValidateApprovalAsync("finance", "xero_post_journal", approvalToken, cancellationToken))
            {
                return ErrorResponse("SENSITIVE_APPROVAL_REQUIRED",
                    "This SENSITIVE operation requires explicit governance approval");
            }
            
            var journal = JsonSerializer.Deserialize<JournalDto>(journalJson);
            if (journal == null)
                return ErrorResponse("INVALID_JOURNAL_JSON", "Failed to parse journal JSON");
            
            _logger.LogError("[XeroTools] Posting journal {Reference}", journal.Reference);
            
            var result = await _xeroClient.PostJournalAsync(journal, cancellationToken);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                data = result,
                message = "Journal posted successfully",
                warning = "This operation directly modified the general ledger",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] PostJournal failed");
            return ErrorResponse("POST_JOURNAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Request approval for a sensitive operation.
    /// Tool: xero_request_approval
    /// This tool initiates the approval gate flow.
    /// </summary>
    [Description("Request approval for a sensitive finance operation")]
    [McpServerTool(Name = "xero_request_approval")]
    public async Task<string> RequestApprovalAsync(
        [Description("Operation name (e.g., 'xero_create_invoice')")] string operation,
        [Description("Operation parameters as JSON")] string parametersJson,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[XeroTools] RequestApproval called for {Operation}", operation);
            
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson) 
                ?? new Dictionary<string, object>();
            
            var approvalToken = await _approvalGate.RequestApprovalAsync(
                "finance",
                operation,
                Configuration.OperationScope.Write,
                parameters,
                cancellationToken);
            
            _logger.LogInformation("[XeroTools] Approval granted with token for {Operation}", operation);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                approvalToken = approvalToken,
                message = "Approval granted. Use token with the operation tool.",
                timestamp = DateTime.UtcNow
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning("[XeroTools] Approval denied for {Operation}: {Reason}", operation, ex.Message);
            return ErrorResponse("APPROVAL_DENIED", ex.Message);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning("[XeroTools] Approval timeout for {Operation}", operation);
            return ErrorResponse("APPROVAL_TIMEOUT", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[XeroTools] RequestApproval failed");
            return ErrorResponse("REQUEST_APPROVAL_FAILED", ex.Message);
        }
    }

    // ======================================================================
    // PRIVATE HELPERS
    // ======================================================================

    private async Task<bool> ValidateApprovalAsync(
        string domain,
        string operation,
        string? approvalToken,
        CancellationToken cancellationToken)
    {
        // If no approval token provided, request one
        if (string.IsNullOrEmpty(approvalToken))
        {
            _logger.LogWarning("[XeroTools] No approval token provided for {Operation}", operation);
            return false;
        }
        
        // Validate the token
        var isValid = await _approvalGate.ValidateApprovalTokenAsync(approvalToken);
        
        if (!isValid)
        {
            _logger.LogWarning("[XeroTools] Invalid approval token for {Operation}", operation);
            return false;
        }
        
        return true;
    }

    private static string ErrorResponse(string errorCode, string message)
    {
        return JsonSerializer.Serialize(new
        {
            success = false,
            errorCode = errorCode,
            message = message,
            timestamp = DateTime.UtcNow
        });
    }
}
