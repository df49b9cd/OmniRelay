# DSL Extensions (Phase 1)

Minimal DSL plug-ins can run inside the OmniRelay data-plane without restarting the process. Wasm/native hosts are deferred; this doc covers the shipped DSL host.

## What a DSL package is
- **Payload:** UTF-8 text; one instruction per line: `OPCODE [arg]`.
- **Opcodes (allowed by default):**
  - `SET text` – replace buffer with `text`.
  - `APPEND text` – append `text`.
  - `UPPER` / `LOWER` – case conversion.
  - `TRUNCATE n` – keep first *n* bytes (UTF-8, not rune-aware).
  - `RET` – return immediately.
- **No side effects:** pure string/buffer transforms; no I/O, no reflection.

## Signing requirements
- Packages must be RSA-signed (PKCS#1 v1.5) with the configured `SignatureAlgorithm` (default SHA-256) and validated against a public key.
- File shape used by the host:
  - `ExtensionPackage(Type: Dsl, Name, Version, Payload, Signature[, Metadata])`
- Example (authoring):
```csharp
using var rsa = RSA.Create(2048);
var payload = "SET hello\nAPPEND  world\nRET"u8.ToArray();
var signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
var package = new ExtensionPackage(ExtensionType.Dsl, "greeting", new Version(1,0), payload, signature);
```
Distribute the public key via configuration; keep the private key offline.

## Running inside the host
```csharp
var options = DslExtensionHostOptions.CreateDefault(HashAlgorithmName.SHA256, publicKeyBytes);
var registry = new ExtensionRegistry();
var host = new DslExtensionHost(options, registry, logger);

if (host.TryLoad(package, out var program))
{
    if (host.TryExecute(ref program, inputSpan, out var output))
    {
        // use output
    }
}
```
- **Budgets (defaults):** max 512 instructions, 32 KB output, 50 ms wall-clock.
- **Failure policy:** `FailOpen` (optional), `ReloadOnFailure` with cooldown. Reload reparses the package; fail-open returns original input after a watchdog trip.
- **Allowlist:** configure `AllowedOpcodes` to restrict what a host can execute.

## Telemetry & diagnostics
- LoggerMessage events: load (100), reject (101), failure (102), watchdog (103), executed (104), reloaded (105).
- Registry-backed snapshot exposed over HTTP inbound:
  - `GET /control/extensions` (alias `/omnirelay/control/extensions`).
  - Schema: `ExtensionDiagnosticsResponse { schemaVersion, generatedAt, extensions[] }` where each entry includes name, version, type, status, lastError, lastWatchdog, lastLoadedAt/ExecutedAt, durationMs, failureCount.
- The registry also implements `IExtensionDiagnosticsProvider` for in-process inspection.

## Limits and current scope
- Only the DSL host is active; Proxy-Wasm and native plugin hosts are deferred (project-board WORK-003B/003C).
- No dynamic loading of assemblies or user code; transformations are deterministic and CPU/memory bounded.
- Keep payloads small and terminate with `RET` to avoid budget trips.

## Troubleshooting
- **Invalid signature:** ensure payload used for signing matches the shipped bytes and key is correct.
- **Watchdog trips:** increase `MaxInstructions/MaxOutputBytes/MaxExecutionTime` cautiously or simplify the program.
- **Disallowed opcode:** add it to `AllowedOpcodes` if you intend to permit it.
