namespace CuriosityStack.Mcp.Core;

public sealed record McpExecutionContext(
    string ActiveTenant,
    string ActiveProject,
    string CorrelationId,
    string Actor,
    DateTime RequestedAtUtc)
{
    public static McpExecutionContext Create(string? activeTenant = null, string? activeProject = null, string? actor = null)
    {
        return new McpExecutionContext(
            ActiveTenant: string.IsNullOrWhiteSpace(activeTenant) ? "local-personal" : activeTenant,
            ActiveProject: string.IsNullOrWhiteSpace(activeProject) ? "default" : activeProject,
            CorrelationId: Guid.NewGuid().ToString("N"),
            Actor: string.IsNullOrWhiteSpace(actor) ? Environment.UserName : actor,
            RequestedAtUtc: DateTime.UtcNow);
    }
}
