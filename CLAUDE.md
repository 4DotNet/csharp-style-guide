# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

An **MCP server that exposes an opinionated C# style guide** — Architectural Decision Records (ADRs) and style-guide documents describing how we think C# code, and C# Web APIs in particular, should be written. MCP clients query the server to retrieve these documents.

## Architecture

The project has two halves — the **content** (Markdown documents) and the **server** (C# MCP tool that serves them):

- **`designs/`** — all the guidance content, authored as documents.
  - **`designs/adr/`** — Architectural Decision Records.
  - **`designs/guidelines/`** — style-guide documents (how to write C# / C# Web APIs).
  - **`designs/index.json`** — a manifest describing the documents under `adr/` and `guidelines/`. This is the entry point the server reads to know what content exists.
- **`src/`** — a C# project that exposes the `designs` content over the **Model Context Protocol**.

### Key design decision: content is fetched from GitHub, not embedded

The server does **not** read the documents from local/hard-coded files. It downloads them at runtime via the **GitHub API** (starting from `designs/index.json`, then the referenced documents). This is deliberate: the guidance can be edited, appended to, and updated on GitHub **without releasing a new build of the tool**. When changing how documents are loaded, preserve this property — new or updated documents committed to `designs/` must be servable without shipping a new version of the server.

## The server project (`src/4dotnet-csharp-style-guide`)

A .NET 10 console app (`net10.0`, assembly name `4dotnet-csharp-style-guide`, root namespace `FourDotNet.CSharpStyleGuide`) that runs as a **stdio** MCP server using the `ModelContextProtocol` SDK.

- **`Program.cs`** — generic-host bootstrap. Binds the `GitHub` config section, registers a typed `HttpClient` against `https://api.github.com/`, and registers the MCP server (name set explicitly to `4dotnet-csharp-style-guide`) with `StyleGuideTools`. Logging goes to **stderr** — stdout is the JSON-RPC channel and must not be written to.
- **`Configuration/GitHubOptions.cs`** — the `GitHub` config section: `ApiBaseUrl` (default `https://api.github.com/`, override for GitHub Enterprise), `Organization` (default `4Dotnet`), `Repository` (default `csharp-style-guide`), `Branch` (default `main`), `DesignsPath` (default `designs`), and an optional `Token` for higher rate limits / private repos. Defaults also live in `appsettings.json`.
- **`Documents/`** — `DocumentDescriptor`/`DocumentIndex`/`RuleDescriptor` (deserialized `index.json`), the service result records in `ServiceModels.cs`, the `IStyleGuideDocumentService` contract, and `GitHubStyleGuideDocumentService`, which fetches `index.json` and each document via the **GitHub Contents API** (`GET repos/{org}/{repo}/contents/{path}?ref={branch}` with `Accept: application/vnd.github.raw+json`). Both the manifest and each document body are cached for **2 hours** (`CacheDuration`); `refresh_cache` clears and re-warms them.
- **`Tools/StyleGuideTools.cs`** — nine MCP tools: `list_documents`, `search_documents` (metadata match), `search_document_contents` (body match with snippets), `get_rules` (flattened rules from the manifest), `find_guidance_for_task` (relevance-ranked), `list_topics` (tag counts), `related_documents` (supersedes/superseded-by + shared tags), `get_document` (full Markdown), and `refresh_cache`. Inactive documents (status `deprecated`/`superseded`, or superseded by another) are excluded unless `includeInactive` is passed.

### DI lifetime note

`GitHubStyleGuideDocumentService` is registered as a **singleton** (via `AddSingleton`) so its in-memory cache is shared across all tool calls. Its HTTP access goes through a **named** `HttpClient` (`GitHubStyleGuideDocumentService.HttpClientName`) resolved per-request from `IHttpClientFactory` — do **not** switch it to a typed `HttpClient` (`AddHttpClient<T>`), which registers the service as transient and silently defeats the cache.

### Manifest format

Each `designs/index.json` entry may declare `rules` (`{ id, rule, severity: required|prohibited|recommended, appliesTo }`) that back `get_rules`, and an optional `supersedes` id that marks the older document inactive and links related documents. `designs/index.schema.json` is the source of truth for the format — keep it in sync when adding fields.

Because documents are fetched from GitHub at runtime, the tools serve whatever is on the configured branch — they will only see documents that have been **committed and pushed**, not local working-tree changes.

### Commands

```bash
# build
dotnet build src/4dotnet-csharp-style-guide/4dotnet-csharp-style-guide.csproj

# run the server (stdio; talk MCP JSON-RPC over stdin/stdout)
dotnet run --project src/4dotnet-csharp-style-guide/4dotnet-csharp-style-guide.csproj
```

There is no test project yet. When one is added, document how to run it (and a single test) here.

## Working notes

- The style guide is meant to be **prescriptive and opinionated** — author concrete rules, not configurable options.
- Keep `designs/index.json` in sync when adding or removing documents in `adr/` or `guidelines/` — it is the manifest the server relies on to discover content.
- Prefer the official .NET MCP SDK (`ModelContextProtocol` NuGet package); consult Microsoft Learn (via the microsoft-docs tools) for MCP-server hosting and GitHub API usage in .NET.
