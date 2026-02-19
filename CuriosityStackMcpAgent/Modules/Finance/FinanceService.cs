using CuriosityStack.Mcp.Core.Storage;

namespace CuriosityStack.Mcp.Finance;

public interface IFinanceService
{
    Task<decimal> GetCashPositionAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetNetWorthAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetMarginExposureAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetMonthlyBurnAsync(CancellationToken cancellationToken = default);
    Task<string> AddManualEntryAsync(string category, decimal amount, string? notes, CancellationToken cancellationToken = default);
}

public sealed class FinanceService : IFinanceService
{
    private readonly ISqliteStore _store;

    public FinanceService(ISqliteStore store)
    {
        _store = store;
    }

    public async Task<decimal> GetCashPositionAsync(CancellationToken cancellationToken = default)
    {
        return await _store.QuerySingleAsync(
            "SELECT COALESCE(SUM(Balance), 0) FROM Accounts WHERE lower(AccountType) = 'cash';",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);
    }

    public async Task<decimal> GetNetWorthAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _store.QuerySingleAsync(
            "SELECT COALESCE(SUM(Balance), 0) FROM Accounts;",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);

        var positions = await _store.QuerySingleAsync(
            "SELECT COALESCE(SUM(MarketValue), 0) FROM Positions;",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);

        return assets + positions;
    }

    public async Task<decimal> GetMarginExposureAsync(CancellationToken cancellationToken = default)
    {
        return await _store.QuerySingleAsync(
            "SELECT COALESCE(SUM(MarginExposure), 0) FROM Positions;",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);
    }

    public async Task<decimal> GetMonthlyBurnAsync(CancellationToken cancellationToken = default)
    {
        var latest = await _store.QuerySingleAsync(
            "SELECT BurnRate FROM CashFlowSnapshots ORDER BY SnapshotDateUtc DESC LIMIT 1;",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);

        if (latest > 0)
        {
            return latest;
        }

        return await _store.QuerySingleAsync(
            "SELECT COALESCE(AVG(Outflows - Inflows), 0) FROM CashFlowSnapshots;",
            r => Convert.ToDecimal(r.GetDouble(0)),
            cancellationToken: cancellationToken);
    }

    public async Task<string> AddManualEntryAsync(string category, decimal amount, string? notes, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        await _store.ExecuteAsync(
            """
            INSERT INTO FinanceManualEntries (Id, Category, Amount, Notes, CreatedAtUtc)
            VALUES (@id, @category, @amount, @notes, @createdAtUtc);
            """,
            new Dictionary<string, object?>
            {
                ["id"] = id,
                ["category"] = category,
                ["amount"] = amount,
                ["notes"] = notes,
                ["createdAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return id;
    }
}
