# Polymer ↔️ yab Interop Harness

This sample spins up a minimal Polymer HTTP server and exercises it with [`yab`](https://github.com/yarpc/yab), Uber's YARPC CLI. It provides a lightweight sanity check that Polymer responds to YARPC-style HTTP calls issued by yab.

## Prerequisites

1. Install `.NET` 8/10 SDK (already required for Polymer).
2. Install `yab` (requires Go):

   ```bash
   go install go.uber.org/yarpc/yab@latest
   export PATH="$PATH:$(go env GOPATH)/bin"
   ```

## Run the demo

```bash
bash tests/Polymer.YabInterop/run-yab.sh
```

The script will:

1. Build the Polymer solution.
2. Launch a simple `echo::ping` Polymer service listening on `http://127.0.0.1:8080`.
3. Invoke the service with `yab` using JSON encoding.
4. Print the response and shut the server down.

You can customise the run via environment variables:

- `PORT` – HTTP port to bind (default `8080`).
- `DURATION` – Seconds to keep the server alive (default `10`).
- `REQUEST_PAYLOAD` – JSON payload sent via `yab` (default `{"message":"hello from yab"}`).
- `CALLER` – Caller name advertised to the server (default `yab-demo`).

Example:

```bash
PORT=9090 REQUEST_PAYLOAD='{"message":"test"}' bash tests/Polymer.YabInterop/run-yab.sh
```

Expected output resembles:

```
Polymer echo server listening on http://127.0.0.1:8080
Issuing yab request...
{"message":"hello from yab"}
"hello from yab"
yab invocation complete
```

## Project layout

- `tests/Polymer.YabInterop/Program.cs` – Minimal Polymer HTTP echo service.
- `tests/Polymer.YabInterop/run-yab.sh` – Helper script to launch the server and issue a `yab` request.

Use this harness as a starting point for richer interop tests or integration into CI.
