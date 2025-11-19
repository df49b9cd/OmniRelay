Working protocol for OmniRelay tasks

1) Load context
- Read memories: project_structure, style_conventions, suggested_commands, done_when_finished, ci_cd (and available_tools if needed).
- Query graph (server-memory) for components/tests/workflows related to the task (e.g., covers/uses/depends_on relations).

2) Plan
- For non-trivial work, run sequential-thinking MCP to outline steps/risks before coding.
- Identify impacted components and the exact test suites to run.

3) Implement
- Follow style_conventions; keep edits minimal and commented only when necessary.
- Update graph if adding/modifying components, test suites, or workflows.

4) Validate
- Run appropriate commands from suggested_commands; at minimum `dotnet build OmniRelay.slnx` and the relevant test projects/suites.
- Note what was executed in the response.

5) Wrap up
- Cross-check done_when_finished checklist.
- Update memories if conventions/processes change; otherwise leave them untouched.
- Summarize changes with file:line refs; suggest next steps if any.

Environment assumptions
- macOS (Darwin), shell zsh; danger-full-access; network enabled; approval policy never.
- CI parity via Dockerfile.ci (`docker build --target ci -f Dockerfile.ci -t omnirelay-ci .`).
