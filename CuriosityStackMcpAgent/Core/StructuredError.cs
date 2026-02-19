namespace CuriosityStack.Mcp.Core;

public sealed record StructuredError(
    string Code,
    string Message,
    string? FailureReason,
    string CorrelationId,
    DateTime TimestampUtc,
    Dictionary<string, object>? Metadata = null);
