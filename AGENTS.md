# Repository Guidelines

## Project Structure & Module Organization
- `src/` houses all production code; core runtime lives in `src/OmniRelay`, configuration binder in `src/OmniRelay.Configuration`, CLI in `src/OmniRelay.Cli`, and codegen in `src/OmniRelay.Codegen.*`.
- `tests/` mirrors those areas with xUnit projects (`OmniRelay.Core.UnitTests`, `OmniRelay.Cli.UnitTests`, `OmniRelay.HyperscaleFeatureTests`, etc.). Interop and yab suites sit in `tests/OmniRelay.YabInterop`.
- `docs/` contains architecture notes and guidance (AOT, diagnostics, samples).
- `eng/` holds repeatable scripts (`run-ci.sh`, `run-aot-publish.sh`, `run-hyperscale-smoke.sh`). Docker recipes live in `docker/`. Runnable samples are under `samples/`.

## Build, Test, and Development Commands
- Restore/build solution: `dotnet build OmniRelay.slnx` (targets .NET 10; respects Directory.Build.* settings).
- Fast unit slice: `dotnet test tests/OmniRelay.Core.UnitTests/OmniRelay.Core.UnitTests.csproj`.
- Full CI parity: `./eng/run-ci.sh` (wraps restore + build + primary test sets).
- Hyperscale/interop container smoke: `docker build -f docker/Dockerfile.hyperscale.ci .` (invokes `eng/run-hyperscale-smoke.sh` internally).
- Native AOT publish: `./eng/run-aot-publish.sh [rid] [Configuration]` (defaults to `linux-x64 Release`).
- CLI development: `dotnet run --project src/OmniRelay.Cli -- --help` for local validation flows.

## Coding Style & Naming Conventions
- C# uses spaces with `indent_size = 4`; file-scoped namespaces; System usings sorted first; braces required even for single statements; newline before braces.
- Prefer `var` for locals; UTF-8 with final newline; trim trailing whitespace.
- Namespaces and packages follow `OmniRelay.*`; public types/members in `PascalCase`, locals/fields in `camelCase`; async methods end with `Async`.
- Keep configuration examples under `docs/` or `samples/`; avoid committing real secrets or environment-specific endpoints.

## Testing Guidelines
- Framework: xUnit across unit, integration, and feature suites. Typical naming: `*Tests.cs` for unit, `*FeatureTests` for broader coverage.
- Run targeted filters with `dotnet test <proj> --filter Category=<name>` when available; keep new tests deterministic (no external network).
- CI reports coverage to Codecov; aim to cover new branches/edge cases when touching transports, middleware, or codecs.
- For transport/interop changes, run `tests/OmniRelay.YabInterop` and the hyperscale Docker recipe before opening a PR.

## Commit & Pull Request Guidelines
- Follow the existing conventional-prefix style seen in history (`feat:`, `fix:`, `chore:`, `docs:`, `revert …`). Keep subject imperative and ≤72 characters; include scope in the body if helpful.
- PRs should link issues/tickets, list user-facing changes and breaking notes, and quote key commands executed (e.g., `dotnet build OmniRelay.slnx; dotnet test …`).
- Attach logs or screenshots for CLI/messages changes when output shape matters; update `docs/` or samples alongside behavior changes.
- Before pushing, ensure `dotnet format`/IDE analyzers are clean per `.editorconfig` and that core/unit suites pass.***

## Agent Startup Prompt
Use this template when starting a new Codex session to load context and follow the working protocol:

```
You are Codex working in /Users/smolesen/Dev/OmniRelay on macOS (Darwin) with zsh.
Approval policy: never; sandbox: danger-full-access; network: enabled.
Use sequential-thinking MCP for any non-trivial task to outline steps/risks.
Follow plan → implement → validate → wrap-up.
Use repo commands from suggested_commands; adhere to style_conventions.
Respond with file:line refs for changes; note tests/commands run.
```

## Desktop Commander Workflow
- Prefer Desktop Commander tooling over ad-hoc `bash` even though the sandbox allows it. Use `mcp__desktop-commander__read_file` / `read_multiple_files` for inspection, `apply_patch` / `mcp__desktop-commander__edit_block` / chunked `write_file` for edits, and `mcp__desktop-commander__start_process` with `interact_with_process` + `read_process_output` for long-running commands to keep the activity log consistent.
- Keep every action observable: favor Desktop Commander commands even for simple listings, record truncated outputs in your notes, and only fall back to direct shell utilities when a provided tool cannot fulfill the need.
- When searching the repo, favor `mcp__desktop-commander__start_search` with `get_more_search_results` / `stop_search` (and `list_searches` when tracking sessions) instead of raw `rg`/`find` so queries remain auditable.
- For onboarding or canonical flows, call `mcp__desktop-commander__get_prompts` to pull the standardized prompt packages rather than crafting ad-hoc instructions.

### Desktop Commander Tool Reference
- `mcp__desktop-commander__create_directory` — create or ensure directories exist before writing files.
- `mcp__desktop-commander__edit_block` — surgically replace targeted snippets inside a file.
- `mcp__desktop-commander__force_terminate` — stop runaway Desktop Commander-managed processes.
- `mcp__desktop-commander__get_config` — inspect the CLI’s sandbox configuration and limits.
- `mcp__desktop-commander__get_file_info` — retrieve metadata (size, timestamps, permissions) for a given path.
- `mcp__desktop-commander__get_more_search_results` — page through outstanding search sessions started via `start_search`.
- `mcp__desktop-commander__get_prompts` — load Codex onboarding prompts (e.g., organize downloads, explain repo).
- `mcp__desktop-commander__get_recent_tool_calls` — review prior tool invocations for context continuity.
- `mcp__desktop-commander__get_usage_stats` — capture aggregated CLI usage metrics for debugging.
- `mcp__desktop-commander__give_feedback_to_desktop_commander` — open the feedback form for the Desktop Commander team.
- `mcp__desktop-commander__interact_with_process` — send commands to an existing REPL/process launched via `start_process`.
- `mcp__desktop-commander__kill_process` — terminate a background REPL/process by PID when `force_terminate` is insufficient.
- `mcp__desktop-commander__list_directory` — enumerate files/folders with depth controls (preferred over `ls`).
- `mcp__desktop-commander__list_processes` — view currently running Desktop Commander-managed processes.
- `mcp__desktop-commander__list_searches` — inspect active file/content searches.
- `mcp__desktop-commander__list_sessions` — see interactive session states (blocked, running, finished).
- `mcp__desktop-commander__move_file` — move/rename files and directories atomically.
- `mcp__desktop-commander__read_file` — read file slices with optional offsets/lengths (cap at ~200 lines per call).
- `mcp__desktop-commander__read_multiple_files` — fetch several files simultaneously when comparing artifacts.
- `mcp__desktop-commander__read_process_output` — capture buffered output from a running process without sending input.
- `mcp__desktop-commander__set_config_value` — update Desktop Commander configuration keys when absolutely required.
- `mcp__desktop-commander__start_process` — launch REPLs or long-lived commands (Python, bash, dotnet, etc.).
- `mcp__desktop-commander__start_search` — run indexed file/content searches with regex/literal support.
- `mcp__desktop-commander__stop_search` — halt an in-flight search to conserve resources.
- `mcp__desktop-commander__write_file` — write/append content in ≤30-line chunks per call.
- `apply_patch` — freeform multi-line patch tool for complex edits not suited to `edit_block`.

## Codex CLI Tools
- Codex CLI mcp tool strategies:
  1. Always chunk `read_file` access: request at most 200 lines (or <10 KiB) per call using `offset`/`length`, and immediately follow with the next chunk until the entire file or log is captured—never rely on a single call for large files.
  2. When inspecting a specific region, read overlapping 200-line windows before and after the target block to preserve surrounding context without breaching the limit.
  3. For long-running tool output (tests, builds), prefer commands that support incremental filters (`grep`, `dotnet test --filter`, etc.) or paging so the returned text stays under the cap while still covering the needed failures.
  4. Document any truncated segments in the final response so subsequent turns know which portions were intentionally skipped and can re-read targeted chunks.
