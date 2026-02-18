using CuriosityStack.Mcp.Configuration;
using CuriosityStack.Mcp.Infrastructure.Governance;
using CuriosityStack.Mcp.Tools.Finance;
using CuriosityStack.Mcp.Tools.Git;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.IO;

// Build the host with MCP server and all dependencies
var builder = Host.CreateApplicationBuilder(args);

// Configure logging with stderr output for STDIO transport compliance
builder.Logging.AddConsole(options =>
{
    // Force all logs to stderr to preserve stdout for JSON-RPC protocol
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Register configuration
builder.Services.Configure<McpAgentSettings>(
    builder.Configuration.GetSection("McpAgent"));

// Register governance and approval gate infrastructure
builder.Services.AddSingleton<IApprovalGateManager, ApprovalGateManager>();
builder.Services.AddSingleton<IGovernancePolicy, DefaultGovernancePolicy>();

// Register HTTP factory and Xero API client
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IXeroApiClient, XeroApiClient>();

// Git client - uses LibGit2Sharp
builder.Services.AddSingleton<IGitRepository, LibGit2SharpRepository>();

// Register tool implementations
builder.Services.AddSingleton<XeroTools>();
builder.Services.AddSingleton<GitTools>();

// Register MCP server and STDIO transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<XeroTools>()
    .WithTools<GitTools>();

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Curiosity Stack MCP Agent starting...");
logger.LogInformation("Finance (Xero) and Git tools initialized");
logger.LogInformation("Governance: Approval gates enabled for Sensitive/Write operations");

try
{
    await host.RunAsync();
}
catch (OperationCanceledException)
{
    logger.LogInformation("MCP agent shut down due to cancellation.");
}
catch (IOException ex) when (
    ex.Message.Contains("pipe", StringComparison.OrdinalIgnoreCase) ||
    ex.Message.Contains("stream", StringComparison.OrdinalIgnoreCase))
{
    logger.LogInformation("MCP STDIO stream closed by client; shutting down cleanly.");
}
