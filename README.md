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
- **Governance approval gates** for write/sensitive actions

It uses `ModelContextProtocol` with STDIO transport and tool registration via MCP attributes.

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
