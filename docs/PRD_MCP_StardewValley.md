Product Requirements Document (PRD)
Model-Context Protocol (MCP) Server for Stardew Valley

Date: 2025-08-29
Author: GitHub Copilot

Summary
- Build an MCP server that provides authoritative, searchable context about Stardew Valley to LLMs and agent workflows. Sources: Stardew Valley wiki (Fandom/MediaWiki), SMAPI docs and manifests, and local game code/data schema. Provide vectorized retrieval, canonical metadata, and tool endpoints to support code-level reasoning about mods, game assets, and APIs.

Objectives
- Enable reliable context retrieval for LLMs answering questions about game mechanics, items, NPCs, and mod APIs.
- Support understanding of SMAPI mod manifests and common mod API patterns.
- Allow ingest/interpretation of game code and data schema without redistributing copyrighted binary assets.
- Provide an extensible ingestion pipeline and simple REST API that follows MCP semantics for context delivery.

Success metrics
- Average context-retrieval latency < 200ms for cached queries.
- Relevant context appears in top-5 results ≥ 90% on curated benchmark of 200 queries.
- End-to-end ingestion for wiki + SMAPI docs + game schema in < 8 hours for first full seed.
- Safe operation: no copyrighted game binaries served; compliance checks in ingestion.

Scope
- In scope: MediaWiki integration (Fandom API), SMAPI manifest parsing, content indexing, vector DB-based similarity search, MCP-compatible context endpoints, CLI ingestion tools, docs.
- Out of scope: Hosting third-party copyrighted game files for download, automatic distribution of decompiled game code, GUI mod management UIs (optional later).

User personas
- LLM/Agent integrators: need concise, relevant context chunks to augment responses.
- Mod authors: query how APIs work and find examples, check compatibility.
- Devs building mod-support tools: need symbol/manifest search and schema references.

Functional requirements
1. Data sources
   - Stardew Valley wiki: use MediaWiki API (search, page content, revisions). Respect API terms and rate limits; avoid scraping HTML when possible.
   - SMAPI: ingest official SMAPI docs (GitHub/website) and parse local mod `manifest.json` files to surface mod metadata and API hooks.
   - Game schema: ingest textual resources (game Data JSON from unpacked assets) and community schema references. If local binary assets (XNB) must be inspected, require explicit local access and convert via community tools (e.g., XNB→JSON) on the client side; do not expose raw binaries.

2. Ingestion & normalization
   - MediaWiki ingestion using MediaWiki API with HTML→text normalization, table extraction, code block preservation.
   - SMAPI processing: index manifest fields (name, author, version, SMAPIVersion), extract API method docs, event hooks, and example code snippets.
   - Game schema mapping: canonicalize entity names, IDs (e.g., item IDs, crop IDs), and map to wiki pages and manifest references.
   - Chunking: 500–1,000 token chunks with overlap; preserve metadata linking back to source URL/file and section anchor.

3. Indexing & retrieval
   - Embeddings + vector DB (configurable: FAISS, Milvus, Pinecone, etc.).
   - Metadata index for filters (source_type, game_version, mod_name, last_updated).
   - API for semantic search with top_k and filter options.

4. MCP API surface (HTTP REST)
   - POST /v1/context/query
     - body: {query, top_k=10, filters: {source_type, game_version, mod_name}}
     - response: [{id, score, text, source: {type, url|file, section}, tokens}]
   - GET /v1/docs/wiki?page={title}
   - POST /v1/ingest/wiki {url|page_title}
   - POST /v1/ingest/mod {manifest_file_path}
   - POST /v1/ingest/game-schema {local_game_path}
   - POST /v1/embeddings/batch {texts[]}
   - Admin: GET /v1/status, POST /v1/reindex
   - Authentication: API keys; TLS required.

5. Developer tooling
   - CLI: `mcp ingest wiki --page "Parsnip"`, `mcp ingest mod --path "mods/MyMod/manifest.json"`, `mcp ingest game --path "C:\Program Files\Stardew Valley"`.
   - Config file: `mcp-config.yml` with vector DB settings, embedding provider, ingestion schedules.
   - Local-only mode: require a flag and explicit consent for reading local game files.

Non-functional requirements
- Performance: typical query <= 300ms (cached); cold start retrieval <1s.
- Availability: 99% for hosted MCP.
- Security: TLS, API keys, role-based admin endpoints, rate limiting.
- Privacy: do not store or redisplay raw game binaries; store only metadata + derived text/embeddings. Respect MediaWiki terms, SMAPI licensing and mod licenses.
- Extensibility: pluggable ingestion connectors and vector DB backends.

Data model (core)
- ContextChunk:
  - id (uuid), text, tokens, embedding_id, source_type (wiki|smapi|game_schema), source_locator (url or absolute path), section_anchor, game_version, mod_name (if applicable), checksum, last_fetched.
- Example MCP response chunk:
  {
    "id": "uuid",
    "score": 0.92,
    "text": "Parsnip: base sell price 35g; grows in spring; ...",
    "source": {"type":"wiki","url":"https://.../Parsnip","#section":"Growth"}
  }

Ingestion pipeline (recommended)
- Fetch → Normalize (HTML→text, tables→structured rows) → Chunk → Deduplicate → Generate embeddings → Store in vector DB + metadata store.
- Periodic incremental updates by revision timestamp for wiki; for mods watch manifest file changes.

SMAPI & game-code considerations
- Prefer canonical public SMAPI docs and community sources over decompiling code.
- For deep code inspection, require local developer mode and an explicit consent workflow; use ILSpy/Xamarin or community tools only for local analysis and never distribute decompiled code.
- Parse `manifest.json` to index API usage, dependencies, and compatibility. Extract example code snippets and correlate with SMAPI event hooks.

Architecture (brief)
- Ingestion workers -> Normalizer -> Embedding service -> Vector DB + Metadata DB -> API (MCP endpoints) -> Optional caching layer (Redis) -> Observability (Prometheus/Grafana).
- Optional LLM-frontend worker to synthesize context into concise responses.

Milestones & timeline (example)
- M1 (2–3 weeks): MediaWiki ingestion, embeddings, vector retrieval, /v1/context/query endpoint (MVP).
- M2 (2–3 weeks): SMAPI parsing + mod manifest ingestion + CLI.
- M3 (3–4 weeks): Local game-schema ingestion, mapping IDs → wiki, richer metadata.
- M4 (2–3 weeks): Robust auth, monitoring, deployment hardening, documentation.

Acceptance criteria
- End-to-end ingestion of wiki pages and retrieval of relevant chunks for benchmark queries.
- SMAPI manifest ingestion surfaces mod metadata and sample API hooks.
- Documentation for CLI and API with examples.
- Security requirements met (TLS, API keys).

Risks & mitigations
- Copyright/licensing: use MediaWiki API; parse only local game files on host machine; do not redistribute binaries or decompiled code.
- Data drift: schedule periodic re-indexing and implement checksum-based diffs.
- Malicious mod content: sandbox ingestion; flag/skip executable code execution.

Open questions
- Preferred default vector DB (FAISS local vs managed)?
- Embedding provider (in-house open models vs cloud embeddings)?
- Governance policy for using and exposing community-contributed docs and code snippets.

Next steps
- Choose vector store and embedding provider.
- Build minimal ingestion for top 50 wiki pages and test retrieval with a 200-query benchmark.
- Implement MVP API and CLI; run internal privacy & legal review for handling local game assets.


