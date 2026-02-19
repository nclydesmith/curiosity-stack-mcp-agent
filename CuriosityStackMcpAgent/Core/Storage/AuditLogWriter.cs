using System.Text.Json;
using CuriosityStack.Mcp.Core;

namespace CuriosityStack.Mcp.Core.Storage;

public interface IAuditLogWriter
{
    Task WriteAsync(
        string correlationId,
        string domain,
        string toolName,
        ScopeClassification scope,
        string sideEffects,
        bool succeeded,
        string? failureReason,
        long durationMs,
        object? metadata,
        CancellationToken cancellationToken = default);
}

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly ISqliteStore _store;

    public AuditLogWriter(ISqliteStore store)
    {
        _store = store;
    }

    public Task WriteAsync(
        string correlationId,
        string domain,
        string toolName,
        ScopeClassification scope,
        string sideEffects,
        bool succeeded,
        string? failureReason,
        long durationMs,
        object? metadata,
        CancellationToken cancellationToken = default)
    {
        return _store.ExecuteAsync(
            """
            INSERT INTO AuditLog (
                Id, CorrelationId, Domain, ToolName, Scope, SideEffects, Succeeded, FailureReason, DurationMs, MetadataJson, OccurredAtUtc
            ) VALUES (
                @id, @correlationId, @domain, @toolName, @scope, @sideEffects, @succeeded, @failureReason, @durationMs, @metadataJson, @occurredAtUtc
            );
            """,
            new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString("N"),
                ["correlationId"] = correlationId,
                ["domain"] = domain,
                ["toolName"] = toolName,
                ["scope"] = (int)scope,
                ["sideEffects"] = sideEffects,
                ["succeeded"] = succeeded ? 1 : 0,
                ["failureReason"] = failureReason,
                ["durationMs"] = durationMs,
                ["metadataJson"] = metadata is null ? null : JsonSerializer.Serialize(metadata),
                ["occurredAtUtc"] = DateTime.UtcNow.ToString("O"),
            },
            cancellationToken);
    }
}
