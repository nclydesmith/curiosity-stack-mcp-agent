using System.Diagnostics;
using System.Text.Json;
using CuriosityStack.Mcp.Core.Storage;
using Microsoft.Extensions.Logging;

namespace CuriosityStack.Mcp.Core.Observability;

public interface IToolExecutionRunner
{
    Task<string> RunAsync<T>(
        string domain,
        string toolName,
        ToolPolicyDescriptor policy,
        McpExecutionContext context,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}

public sealed class ToolExecutionRunner : IToolExecutionRunner
{
    private readonly IAuditLogWriter _auditLog;
    private readonly ILogger<ToolExecutionRunner> _logger;

    public ToolExecutionRunner(IAuditLogWriter auditLog, ILogger<ToolExecutionRunner> logger)
    {
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<string> RunAsync<T>(
        string domain,
        string toolName,
        ToolPolicyDescriptor policy,
        McpExecutionContext context,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var metadata = new Dictionary<string, object>
        {
            ["domain"] = domain,
            ["toolName"] = toolName,
            ["scope"] = policy.Scope.ToString(),
            ["requiredApproval"] = policy.RequiredApproval.ToString(),
            ["idempotent"] = policy.IsIdempotent,
            ["activeTenant"] = context.ActiveTenant,
            ["activeProject"] = context.ActiveProject,
            ["actor"] = context.Actor,
        };

        _logger.LogInformation(
            "ToolExecutionStart Domain={Domain} Tool={Tool} CorrelationId={CorrelationId} Scope={Scope}",
            domain,
            toolName,
            context.CorrelationId,
            policy.Scope);

        try
        {
            var data = await action(cancellationToken);
            sw.Stop();

            await _auditLog.WriteAsync(
                context.CorrelationId,
                domain,
                toolName,
                policy.Scope,
                policy.ExpectedSideEffects,
                succeeded: true,
                failureReason: null,
                durationMs: sw.ElapsedMilliseconds,
                metadata,
                cancellationToken);

            return JsonSerializer.Serialize(new
            {
                success = true,
                correlationId = context.CorrelationId,
                durationMs = sw.ElapsedMilliseconds,
                data,
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "ToolExecutionFailed Domain={Domain} Tool={Tool} CorrelationId={CorrelationId} DurationMs={DurationMs}",
                domain,
                toolName,
                context.CorrelationId,
                sw.ElapsedMilliseconds);

            await _auditLog.WriteAsync(
                context.CorrelationId,
                domain,
                toolName,
                policy.Scope,
                policy.ExpectedSideEffects,
                succeeded: false,
                failureReason: ex.Message,
                durationMs: sw.ElapsedMilliseconds,
                metadata,
                cancellationToken);

            var error = new StructuredError(
                Code: "TOOL_EXECUTION_FAILED",
                Message: ex.Message,
                FailureReason: ex.GetType().Name,
                CorrelationId: context.CorrelationId,
                TimestampUtc: DateTime.UtcNow,
                Metadata: metadata);

            return JsonSerializer.Serialize(new
            {
                success = false,
                correlationId = context.CorrelationId,
                durationMs = sw.ElapsedMilliseconds,
                error,
            });
        }
    }
}
