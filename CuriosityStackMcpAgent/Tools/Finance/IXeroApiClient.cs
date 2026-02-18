using CuriosityStack.Mcp.Models;

namespace CuriosityStack.Mcp.Tools.Finance;

/// <summary>
/// Contract for interacting with Xero API.
/// Implements read-only and write operations with proper scoping.
/// </summary>
public interface IXeroApiClient
{
    // ReadOnly Operations
    Task<TrialBalanceDto> GetTrialBalanceAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto[]> GetInvoicesByContactAsync(string contactId, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);
    Task<AccountDto[]> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<AccountDto?> GetAccountByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<ContactDto[]> GetContactsAsync(CancellationToken cancellationToken = default);

    // Write Operations
    Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto invoice, CancellationToken cancellationToken = default);
    Task<InvoiceDto> UpdateInvoiceAsync(string invoiceId, InvoiceDto invoice, CancellationToken cancellationToken = default);
    Task<InvoiceDto> AuthoriseInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default);

    // Sensitive Operations
    Task<JournalDto> PostJournalAsync(JournalDto journal, CancellationToken cancellationToken = default);
    Task<AccountDto> UpdateAccountAsync(string accountCode, AccountDto account, CancellationToken cancellationToken = default);

    // OAuth Management
    Task RefreshTokenAsync(CancellationToken cancellationToken = default);
    Task<string> GetActiveTenantAsync(CancellationToken cancellationToken = default);
}