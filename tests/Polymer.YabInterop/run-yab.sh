#!/usr/bin/env bash
set -euo pipefail

PORT="${PORT:-8080}"
DURATION="${DURATION:-10}"
REQUEST_PAYLOAD="${REQUEST_PAYLOAD:-{\"message\":\"hello from yab\"}}"
CALLER="${CALLER:-yab-demo}"

if ! command -v yab >/dev/null 2>&1; then
  echo "yab executable not found in PATH. Install it via 'go install go.uber.org/yarpc/yab@latest'." >&2
  exit 1
fi

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
PROJECT_DIR=$(cd "$SCRIPT_DIR/../../" && pwd)

pushd "$PROJECT_DIR" >/dev/null

dotnet build --nologo >/dev/null

dotnet run --project tests/Polymer.YabInterop/Polymer.YabInterop.csproj -- --port "$PORT" --duration "$DURATION" &
SERVER_PID=$!
trap 'kill $SERVER_PID >/dev/null 2>&1 || true' EXIT

sleep 2

echo "Issuing yab request..."
yab --http --peer "http://127.0.0.1:${PORT}" --service echo --procedure echo::ping --encoding json --request "$REQUEST_PAYLOAD" --caller "$CALLER" --timeout 2s

echo "yab invocation complete"

wait $SERVER_PID 2>/dev/null || true

popd >/dev/null
