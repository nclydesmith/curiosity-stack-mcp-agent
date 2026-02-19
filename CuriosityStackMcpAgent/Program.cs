using CuriosityStack.Mcp.Configuration;
using CuriosityStack.Mcp.Core.Governance;
using CuriosityStack.Mcp.Core.Observability;
using CuriosityStack.Mcp.Core.Security;
using CuriosityStack.Mcp.Core.Storage;
using CuriosityStack.Mcp.Infrastructure.Governance;
using CuriosityStack.Mcp.Finance;
using CuriosityStack.Mcp.Health;
using CuriosityStack.Mcp.Journal;
using CuriosityStack.Mcp.Projects;
using CuriosityStack.Agent.LifeOrchestrator;
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

builder.Services.Configure<SqliteOptions>(options =>
{
    options.DatabasePath = builder.Configuration["McpAgent:Personal:SqlitePath"] ?? "data/personal-mcp.db";
});

// Register governance and approval gate infrastructure
builder.Services.AddSingleton<IApprovalGateManager, ApprovalGateManager>();
builder.Services.AddSingleton<IGovernancePolicy, DefaultGovernancePolicy>();

// Personal MCP core infrastructure
builder.Services.AddSingleton<ISensitiveDataProtector, SensitiveDataProtector>();
builder.Services.AddSingleton<ISqliteStore, SqliteStore>();
builder.Services.AddSingleton<ISchemaMigrator, SchemaMigrator>();
builder.Services.AddSingleton<IAuditLogWriter, AuditLogWriter>();
builder.Services.AddSingleton<IApprovalTokenService, ApprovalTokenService>();
builder.Services.AddSingleton<IToolPolicyEnforcer, ToolPolicyEnforcer>();
builder.Services.AddSingleton<IToolExecutionRunner, ToolExecutionRunner>();

// Register HTTP factory and Xero API client
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IXeroApiClient, XeroApiClient>();

// Git client - uses LibGit2Sharp
builder.Services.AddSingleton<IGitRepository, LibGit2SharpRepository>();

// Personal domain services
builder.Services.AddSingleton<IFinanceService, FinanceService>();
builder.Services.AddSingleton<IHealthService, HealthService>();
builder.Services.AddSingleton<IProjectsService, ProjectsService>();
builder.Services.AddSingleton<IJournalService, JournalService>();
builder.Services.AddSingleton<ILifeIntentRouter, LifeIntentRouter>();

// Register tool implementations
builder.Services.AddSingleton<XeroTools>();
builder.Services.AddSingleton<GitTools>();
builder.Services.AddSingleton<FinanceTools>();
builder.Services.AddSingleton<HealthTools>();
builder.Services.AddSingleton<ProjectsTools>();
builder.Services.AddSingleton<JournalTools>();
builder.Services.AddSingleton<GovernanceTools>();
builder.Services.AddSingleton<LifeOrchestratorTools>();

// Register MCP server and STDIO transport
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<XeroTools>()
    .WithTools<GitTools>()
    .WithTools<FinanceTools>()
    .WithTools<HealthTools>()
    .WithTools<ProjectsTools>()
    .WithTools<JournalTools>()
    .WithTools<GovernanceTools>()
    .WithTools<LifeOrchestratorTools>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<ISchemaMigrator>();
    await migrator.MigrateAsync();
}

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
