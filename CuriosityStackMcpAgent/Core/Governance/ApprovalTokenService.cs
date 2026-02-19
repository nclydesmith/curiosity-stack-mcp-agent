using System.Security.Cryptography;
using System.Text;
using CuriosityStack.Mcp.Core.Storage;

namespace CuriosityStack.Mcp.Core.Governance;

public interface IApprovalTokenService
{
    Task<string> IssueTokenAsync(string domain, string toolName, ScopeClassification scope, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<bool> ValidateTokenAsync(string token, string domain, string toolName, ScopeClassification scope, CancellationToken cancellationToken = default);
}

public sealed class ApprovalTokenService : IApprovalTokenService
{
    private readonly ISqliteStore _store;
    private readonly byte[] _secret;

    public ApprovalTokenService(ISqliteStore store)
    {
        _store = store;
        _secret = SHA256.HashData(Encoding.UTF8.GetBytes(Environment.MachineName + "::curiosity-stack::approval"));
    }

    public async Task<string> IssueTokenAsync(string domain, string toolName, ScopeClassification scope, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var tokenId = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.Add(ttl);
        var payload = $"{tokenId}|{domain}|{toolName}|{(int)scope}|{expiresAt:O}";
        var signature = ComputeSignature(payload);
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload + "|" + signature));

        await _store.ExecuteAsync(
            """
            INSERT INTO ApprovalRecords (TokenId, Domain, ToolName, Scope, ExpiresAtUtc, ApprovedAtUtc, IsConsumed)
            VALUES (@tokenId, @domain, @toolName, @scope, @expiresAtUtc, @approvedAtUtc, 0);
            """,
            new Dictionary<string, object?>
            {
                ["tokenId"] = tokenId,
                ["domain"] = domain,
                ["toolName"] = toolName,
                ["scope"] = (int)scope,
                ["expiresAtUtc"] = expiresAt.ToString("O"),
                ["approvedAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);

        return token;
    }

    public async Task<bool> ValidateTokenAsync(string token, string domain, string toolName, ScopeClassification scope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
        }
        catch
        {
            return false;
        }

        var segments = decoded.Split('|');
        if (segments.Length != 6)
        {
            return false;
        }

        var tokenId = segments[0];
        var tokenDomain = segments[1];
        var tokenTool = segments[2];
        var tokenScope = int.TryParse(segments[3], out var scopeValue) ? scopeValue : -1;
        var tokenExpiry = DateTime.TryParse(segments[4], out var expiry) ? expiry : DateTime.MinValue;
        var tokenSignature = segments[5];

        var payload = string.Join('|', segments.Take(5));
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(tokenSignature),
                Encoding.UTF8.GetBytes(ComputeSignature(payload))))
        {
            return false;
        }

        if (!string.Equals(tokenDomain, domain, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(tokenTool, toolName, StringComparison.OrdinalIgnoreCase) ||
            tokenScope != (int)scope ||
            tokenExpiry <= DateTime.UtcNow)
        {
            return false;
        }

        var dbRecord = await _store.QuerySingleAsync(
            """
            SELECT IsConsumed, ExpiresAtUtc
            FROM ApprovalRecords
            WHERE TokenId = @tokenId AND Domain = @domain AND ToolName = @toolName AND Scope = @scope;
            """,
            reader => new
            {
                IsConsumed = reader.GetInt32(0),
                ExpiresAtUtc = DateTime.Parse(reader.GetString(1)),
            },
            new Dictionary<string, object?>
            {
                ["tokenId"] = tokenId,
                ["domain"] = domain,
                ["toolName"] = toolName,
                ["scope"] = (int)scope,
            },
            cancellationToken);

        if (dbRecord is null || dbRecord.IsConsumed == 1 || dbRecord.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return false;
        }

        await _store.ExecuteAsync(
            "UPDATE ApprovalRecords SET IsConsumed = 1 WHERE TokenId = @tokenId;",
            new Dictionary<string, object?> { ["tokenId"] = tokenId },
            cancellationToken);

        return true;
    }

    private string ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_secret);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
