# MCP Server for Stardew Valley

Lightweight MCP server to provide searchable, LLM-friendly context for Stardew Valley: wiki pages, SMAPI docs, and local game schema. See `docs/PRD_MCP_StardewValley.md` and `docs/Actionable_Checklist_MCP_StardewValley.md` for design and plans.

## Prerequisites
- .NET SDK 8.0+ (dotnet CLI)
- Optional: GitHub CLI (`gh`) for repo creation

## Quick start (local)

1. Restore and build

```powershell
dotnet restore
dotnet build
```

2. Run the API (from repo root)

```powershell
dotnet run --project src\McpServer.Api
```

3. Run tests

```powershell
dotnet test test\McpServer.Core.Tests\McpServer.Core.Tests.csproj
```

## Contribution
- Run tests locally and open a PR for review.
- See `CONTRIBUTING.md` for more details.

## Where to find design docs
- PRD: `docs/PRD_MCP_StardewValley.md`
- Checklist: `docs/Actionable_Checklist_MCP_StardewValley.md`

