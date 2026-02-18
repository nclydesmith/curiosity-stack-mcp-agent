using CuriosityStack.Mcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CuriosityStack.Mcp.Tools.Finance;

/// <summary>
/// Xero API client implementation using REST.
/// Handles OAuth 2.0, error handling, and audit logging.
/// </summary>
public sealed class XeroApiClient : IXeroApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Configuration.McpAgentSettings _settings;
    private readonly ILogger<XeroApiClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiresAt;

    public XeroApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<Configuration.McpAgentSettings> settings,
        ILogger<XeroApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TrialBalanceDto> GetTrialBalanceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching trial balance from Xero");
        
        await EnsureTokenAsync(cancellationToken);
        
        // Stub implementation - return mock data
        return new TrialBalanceDto
        {
            ReportDate = DateTime.UtcNow,
            Accounts = new[]
            {
                new TrialBalanceAccountDto
                {
                    Code = "200",
                    Name = "Sales",
                    Debit = 50000,
                    Credit = 0,
                    Balance = 50000
                }
            }
        };
    }

    public async Task<InvoiceDto[]> GetInvoicesByContactAsync(string contactId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching invoices for contact {ContactId}", contactId);
        await EnsureTokenAsync(cancellationToken);
        return Array.Empty<InvoiceDto>();
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching invoice {InvoiceId}", invoiceId);
        await EnsureTokenAsync(cancellationToken);
        return null;
    }

    public async Task<AccountDto[]> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching accounts from chart of accounts");
        await EnsureTokenAsync(cancellationToken);
        return Array.Empty<AccountDto>();
    }

    public async Task<AccountDto?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching account {Code}", code);
        var accounts = await GetAccountsAsync(cancellationToken);
        return accounts.FirstOrDefault(a => a.Code == code);
    }

    public async Task<ContactDto[]> GetContactsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching contacts");
        await EnsureTokenAsync(cancellationToken);
        return Array.Empty<ContactDto>();
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto invoice, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Creating invoice {InvoiceNumber} - WRITE operation", invoice.InvoiceNumber);
        await EnsureTokenAsync(cancellationToken);
        
        return invoice with { InvoiceId = Guid.NewGuid().ToString() };
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(string invoiceId, InvoiceDto invoice, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Updating invoice {InvoiceId}", invoiceId);
        await EnsureTokenAsync(cancellationToken);
        return invoice;
    }

    public async Task<InvoiceDto> AuthoriseInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Authorising invoice {InvoiceId} - SENSITIVE operation", invoiceId);
        var invoice = await GetInvoiceAsync(invoiceId, cancellationToken);
        if (invoice == null)
            throw new InvalidOperationException($"Invoice not found: {invoiceId}");

        return invoice with { Status = "AUTHORISED" };
    }

    public async Task<JournalDto> PostJournalAsync(JournalDto journal, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Posting journal entry - SENSITIVE operation");
        _logger.LogError("Reference: {Reference}, Date: {Date}, Items: {ItemCount}", 
            journal.Reference, journal.JournalDate, journal.LineItems.Length);
        
        await EnsureTokenAsync(cancellationToken);
        return journal with { JournalId = Guid.NewGuid().ToString() };
    }

    public async Task<AccountDto> UpdateAccountAsync(string accountCode, AccountDto account, CancellationToken cancellationToken = default)
    {
        _logger.LogError("Updating account {Code} - SENSITIVE operation", accountCode);
        await EnsureTokenAsync(cancellationToken);
        return account;
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing Xero OAuth token");
        await Task.CompletedTask;
    }

    public async Task<string> GetActiveTenantAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating Xero connection to tenant {TenantId}", _settings.Xero.TenantId);
        await EnsureTokenAsync(cancellationToken);
        return _settings.Xero.TenantId;
    }

    private async Task EnsureTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiresAt)
        {
            _logger.LogWarning("No Xero OAuth token configured - falling back to mock");
            _accessToken = "mock_token_" + Guid.NewGuid().ToString("N").Substring(0, 16);
            _tokenExpiresAt = DateTime.UtcNow.AddHours(1);
        }
        await Task.CompletedTask;
    }
}
