Actionable Checklist — MCP server for Stardew Valley

Reference: `docs/PRD_MCP_StardewValley.md`

Purpose
- Short, actionable task list organized to deliver an MVP and iterate toward the full PRD.

Priority legend
- P0: Required for MVP
- P1: Important for production hardening
- P2: Nice-to-have / future work

Project setup (P0)
- [x] Create repository skeleton and license (README, CODEOWNERS, CONTRIBUTING) (owner: infra) — 1 day (src scaffold created)
- [x] Choose primary language & web framework (C# / .NET 8, ASP.NET Core minimal APIs) (owner: lead) — done
- [ ] Establish branch strategy and CI pipeline (lint, tests) (owner: infra) — 1–2 days (CI not configured)
- [x] Create initial src scaffolding: `src/McpServer.Core`, `src/McpServer.Api` (owner: dev) — done
- [x] Add `docs/PRD_MCP_StardewValley.md` to repo and link to checklist (owner: PM) — done

Embeddings & vector store (P0)
- [x] Choose vector DB backend for MVP (in-memory for prototyping; FAISS later) (owner: lead ML) — decided (in-memory)
- [x] Choose embedding provider and implement adapter (DeterministicEmbeddingsProvider implemented) (owner: lead ML) — done
- [ ] Add `mcp-config.yml` with embedding + vector DB config (owner: dev) — 0.5 day
- [x] Implement local dev instance of vector DB and verify storing/retrieving vectors (InMemoryVectorStore implemented) (owner: dev) — done

MediaWiki ingestion (MVP, P0)
- [x] Implement MediaWiki connector using Fandom/MediaWiki API respecting rate limits (owner: dev) — implemented (`MediaWikiIngestor`)
- [x] Implement normalizer: HTML->text, extract tables, preserve code blocks (owner: dev) — partial: used MediaWiki `extracts` plaintext for MVP
- [x] Implement chunking (500–1,000 token chunks, overlap) and metadata attachment (owner: dev) — `SimpleChunker` implemented
- [x] Generate embeddings and store chunks in vector DB; include metadata (owner: dev) — implemented (`/v1/ingest/wiki/store` endpoint)
- [ ] CLI: `mcp ingest wiki --page "Parsnip"` (owner: dev) — 0.5 day (not implemented)
- Acceptance criterion: top-5 retrieval accuracy >= 80% on a small local benchmark of 50 queries

API & MCP endpoints (MVP, P0)
- [x] Implement POST `/v1/context/query` (query, top_k, filters) (owner: backend) — implemented (basic exact-match fallback)
- [x] Implement semantic search POST `/v1/context/search` using embeddings + vector store (owner: backend) — implemented
- [x] Implement POST `/v1/ingest/wiki` (basic fetch), `/v1/ingest/wiki/preview`, and `/v1/ingest/wiki/store` (store embeddings & metadata) (owner: backend) — implemented
- [ ] Implement GET `/v1/docs/wiki` (owner: backend) — 1–2 days (not implemented)
- [ ] Add basic metadata filtering support (source_type, game_version, mod_name) (owner: backend) — 1 day
- [ ] Add API key auth + TLS enforcement (P1) (owner: infra) — 2 days
- Acceptance criterion: API returns relevant chunks and respects filters; measured latency < 300ms for cached queries

SMAPI & mod manifest ingestion (M1, P0)
- [ ] Implement parser for `manifest.json` and metadata extraction (name, author, version, SMAPIVersion) (owner: dev) — 1 day
- [ ] Extract and index API method docs, event hooks, and example code snippets (owner: dev) — 1–2 days
- [ ] CLI: `mcp ingest mod --path "mods/MyMod/manifest.json"` (owner: dev) — 0.5 day
- Acceptance criterion: manifests indexed and discoverable via `/v1/context/query`

Game schema & local assets ingestion (M2, P1)
- [ ] Implement local-only ingestion mode with explicit consent flag (owner: dev) — 0.5 day
- [ ] Integrate or document recommended conversion tools to extract XNB→JSON (client-side only) (owner: dev/ops) — 1–2 days
- [ ] Implement mapping of item/entity IDs to wiki pages and add cross-links in metadata (owner: dev) — 1–2 days
- Acceptance criterion: Local schema ingestion produces searchable chunks without storing raw binaries

Developer tooling & CLI (P0)
- [ ] CLI commands for wiki/mod/game ingestion plus status and reindex operations (owner: dev) — 2 days
- [ ] Add config-based toggles for local-only mode, rate-limits, and vector DB options (owner: dev) — 1 day
- [x] Add unit tests and test project (NUnit) — done: `test/McpServer.Core.Tests` (owner: dev)
- [x] Added unit tests for processing & embeddings (owner: dev) — done

Testing & benchmark (P0/P1)
- [x] Add unit tests for InMemoryContextService and run locally (owner: dev) — done
- [x] Add unit tests for processing & embeddings (owner: dev) — done (6 tests)
- [x] Create automated benchmark suite (200-query set) from PRD examples and wiki pages (owner: QA) — scaffolded: `tools/McpServer.Benchmark` (sample seed + queries)
- [x] Implement integration tests for connectors and MCP endpoints (owner: QA/dev) — done (`test/McpServer.Core.Tests/Integration/IngestStoreSearchTests.cs`)
- [ ] Performance tests for latency and caching (owner: QA) — 1–2 days

Security, licensing & privacy (P1)
- [ ] Document legal constraints for MediaWiki, SMAPI, and community content; run internal review (owner: legal/PM) — 2–3 days
- [ ] Ensure no raw game binaries or decompiled code are stored or served (owner: infra/dev) — 0.5 day
- [ ] Implement role-based admin endpoints and rate limiting (owner: infra) — 1–2 days

Observability & operations (P1)
- [ ] Add logging, metrics, and health endpoints (Prometheus) (owner: infra) — 1–2 days
- [ ] Configure alerting and dashboards (owner: infra) — 1–2 days
- [ ] Implement incremental reindexing and scheduled jobs (owner: infra/dev) — 1 day

Documentation & examples (P0/P1)
- [ ] API docs with example `curl` and SDK usage (owner: docs) — 1–2 days
- [ ] CLI usage examples and `mcp-config.yml` reference (owner: docs) — 0.5–1 day
- [ ] Quickstart: seed top 50 wiki pages ingestion and example queries (owner: docs/dev) — 1 day

Milestones (mapping to PRD)
- M1 (2–3 weeks): Complete MediaWiki ingestion, vector DB, embeddings, `/v1/context/query` (target: P0 tasks above)
- M2 (2–3 weeks): SMAPI parsing, manifest ingestion, CLI
- M3 (3–4 weeks): Local game-schema ingestion, cross-links, richer metadata
- M4 (2–3 weeks): Auth, monitoring, production hardening, docs

Quick first-week plan (concrete)
- Day 1: Repo skeleton, choose vector DB & embedding provider, implement config file — started (language chosen: .NET; src scaffold created)
- Day 2–3: Implement MediaWiki connector + normalizer; implement chunking and local storage of chunks — done
- Day 4: Integrate embeddings and vector DB; basic `context/query` that returns nearest chunks — done (end-to-end ingest→chunk→embed→store implemented)
- Day 5: Run 50-query retrieval benchmark and iterate on chunking/normalization — next

New immediate action items (today)
- [x] Implement MediaWiki connector skeleton (owner: dev) — done
- [x] Implement chunking & normalization utility (owner: dev) — done
- [x] Wire basic ingestion path: ingest wiki page → chunk → embed → store in InMemory/FAISS (owner: dev) — done (InMemory)
- [x] Add integration test for ingest→store→search (owner: QA/dev) — done
- [x] Add `tools/McpServer.Benchmark` scaffold for retrieval benchmarking (owner: dev) — done (sample data included)
- [ ] Add CLI command and examples for `mcp ingest wiki` (owner: dev)
- [ ] Add `mcp-config.yml` and document embedding + vector DB configuration (owner: dev)

Notes & acceptance criteria summary
- P0 acceptance: End-to-end ingestion + retrieval for wiki with top-5 retrieval ≥ 80% on small benchmark; `/v1/context/query` responds and returns metadata-backed chunks; manifests ingestable and searchable.


