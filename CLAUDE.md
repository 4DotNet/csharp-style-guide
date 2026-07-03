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

## Current state

Repository is an early scaffold — as of the initial commit only `README.md`, `LICENSE`, and `.gitignore` exist; the `designs/` content and `src/` server are not created yet. Once the `src/` project exists, update this file with the real build/test/run commands (`dotnet build`, `dotnet test`, running a single test, launching the MCP server) and the concrete project layout.

## Working notes

- The style guide is meant to be **prescriptive and opinionated** — author concrete rules, not configurable options.
- Keep `designs/index.json` in sync when adding or removing documents in `adr/` or `guidelines/` — it is the manifest the server relies on to discover content.
- Prefer the official .NET MCP SDK (`ModelContextProtocol` NuGet package); consult Microsoft Learn (via the microsoft-docs tools) for MCP-server hosting and GitHub API usage in .NET.
