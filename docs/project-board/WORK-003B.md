# WORK-003B – Proxy-Wasm Runtime Selection & ABI Support

## Goal
Support Proxy-Wasm ABI 0.2.x with selectable runtime (V8 default; Wasmtime/WAMR if built), including capability signaling.

## Scope
- Runtime selection config/build flags; capability advertisement of available runtimes.
- Load/instantiate Wasm modules with signature verification (reusing registry manifest data).
- Basic watchdogs for CPU/memory per VM.

## Acceptance Criteria
- Wasm module loads and runs sample filters in all modes.
- Capability flags reflect runtime availability; incompatible modules rejected.
- Watchdog breach enforces configured policy.

## Status
Open
