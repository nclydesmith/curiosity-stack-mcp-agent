# MCP Proto

Top-level workspace for the Curiosity Stack MCP Agent implementation.

## Workspace Contents

- `MCP Proto.sln` — solution file
- `CuriosityStackMcpAgent/` — .NET 8 MCP server project
- `AGENT_LOG.md` — implementation and stabilization log

## Project Overview

`CuriosityStackMcpAgent` is a protocol-governed MCP server that exposes:
- **Finance domain tools** (Xero-oriented operations)
- **Git domain tools** (repository operations)
- **Personal MCP domain tools** (Finance, Health, Projects, Journal)
- **Life orchestrator tool** (`life.status`) for deterministic cross-domain aggregation
- **Governance approval gates** for write/sensitive actions
- **SQLite-backed auditable persistence** with startup schema migration

It uses `ModelContextProtocol` with STDIO transport and tool registration via MCP attributes.

## Personal MCP Update (2026-02-19)

The Personal MCP layer has been added as deterministic infrastructure (not prompt-only behavior):

- Shared core infrastructure under `CuriosityStackMcpAgent/Core/`
	- scope classification, policy metadata, execution context, structured errors
	- approval tokens with expiry and one-time consumption
	- tool execution observability with correlation IDs and audit records
	- SQLite abstraction and versioned schema migration runner
- Domain modules under `CuriosityStackMcpAgent/Modules/`
	- `Finance`: `finance.cash_position`, `finance.net_worth`, `finance.margin_exposure`, `finance.monthly_burn`, `finance.add_manual_entry`
	- `Health`: `health.weight_trend`, `health.training_load`, `health.recovery_score`, `health.log_weight`, `health.log_training_session`
	- `Projects`: `projects.active`, `projects.deadlines`, `projects.velocity`, `projects.risk_flags`, `projects.create`, `projects.update_status`, `projects.archive`
	- `Journal`: `journal.add_entry`, `journal.log_decision`, `journal.pattern_analysis`, `journal.goal_alignment`
- Orchestrator under `CuriosityStackMcpAgent/Agent/`
	- `life.status` aggregates finance cash position, project deadlines, and health recovery score

Sensitive/write approval workflow:
- request token: `governance.request_approval`
- execute guarded tool with `approvalToken`

Local mode defaults:
- transport: stdio
- database: `data/personal-mcp.db`

## Quick Start

From workspace root:

```powershell
cd "CuriosityStackMcpAgent"
dotnet build
```

Run as MCP server (STDIO):

```powershell
dotnet run --project "CuriosityStackMcpAgent"
```

## Notes on STDIO Runtime

This server expects JSON-RPC messages over STDIO from an MCP client.
Running it directly in a normal terminal can produce parse/stream-close logs when stdin is not MCP-formatted input.

## Git Status

Repository is initialized locally with ordered commits.
Remote is configured but push requires the target GitHub repository to exist and be accessible.

## Next Recommended Steps

1. Create/verify GitHub repo for `origin`.
2. Push branch:

```powershell
git push -u origin main
```

3. Add a minimal MCP smoke test client to exercise one finance and one git tool end-to-end.

## Detailed Docs

- Project-level documentation: `CuriosityStackMcpAgent/README.md`
- Full implementation log: `AGENT_LOG.md`
