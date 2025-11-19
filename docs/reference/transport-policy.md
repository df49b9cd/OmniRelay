# OmniRelay Transport Policy Engine

WORK-001 introduces a governance layer for mesh-internal transports so diagnostics/control-plane endpoints remain HTTP/3-first with protobuf encodings. This document summarizes the configuration surface, CLI workflows, and telemetry you can use to enforce the policy.

## Configuration

Add a `transportPolicy` section under the `omnirelay` root. The engine ships with immutable defaults:

- `control-plane` category allows only `grpc` transport with `protobuf` encoding.
- `diagnostics` category allows `http3` transport with `json` or `protobuf` encoding.

You can refine or override the defaults per cluster:

```json
{
  "omnirelay": {
    "service": "mesh.control",
    "transportPolicy": {
      "categories": {
        "diagnostics": {
          "allowedTransports": ["http3", "http2"],
          "allowedEncodings": ["json", "protobuf"],
          "preferredTransport": "http3",
          "requirePreferredTransport": true
        }
      },
      "exceptions": [
        {
          "name": "legacy-json-http2",
          "category": "diagnostics",
          "appliesTo": ["diagnostics:http"],
          "transports": ["http2"],
          "encodings": ["json"],
          "reason": "Regional observability stack lacks QUIC",
          "expiresAfter": "2025-12-31T00:00:00Z",
          "approvedBy": "transport-governance"
        }
      ]
    }
  }
}
```

Endpoints are referenced by stable names:

| Endpoint | Category | Description |
| --- | --- | --- |
| `diagnostics:http` | `diagnostics` | HTTP listener exposing `/omnirelay/control/*`, docs, probes. |
| `control-plane:grpc` | `control-plane` | MeshKit leadership/shard control gRPC endpoint. |

## CLI Validation

Use `omnirelay mesh config validate` to evaluate layered configs (including `--set` overrides) before publishing:

```bash
omnirelay mesh config validate \
  --config config/appsettings.json \
  --set omnirelay:diagnostics:controlPlane:httpRuntime:enableHttp3=true
```

- Exit code `0` means the transport policy is satisfied.
- Exit code `1` indicates at least one violation; the CLI prints actionable hints (e.g., enable HTTP/3 or register an exception).
- Pass `--format json` for CI/automation pipelines:
  ```bash
  omnirelay mesh config validate --config appsettings.json --format json | jq .
  ```

`omnirelay config validate` still performs full dispatcher binding; use the mesh command when you need fast policy feedback without TLS certificates or inbounds present.

## Telemetry

The engine records metrics via the `OmniRelay.Transport.Policy` meter:

| Metric | Description | Key Tags |
| --- | --- | --- |
| `omnirelay.transport.policy.endpoints` | Number of evaluated endpoints | `omnirelay.transport.endpoint`, `omnirelay.transport.category`, `omnirelay.transport.status` |
| `omnirelay.transport.policy.violations` | Violations detected at startup | Same as above |
| `omnirelay.transport.policy.exceptions` | Approved exceptions used at runtime | Same as above |
| `omnirelay.http.client.fallbacks` | HTTP/3 downgrade counter (existing metric) | `rpc.service`, `rpc.procedure`, `http.observed_protocol` |

Add these meters to your dashboards to visualize downgrade ratios over time and alert when exceptions linger past their expiration date.

## Samples

`samples/Observability.CliPlayground/appsettings.json` now includes a policy exception that keeps local JSON collectors on HTTP/2 while still documenting the expiry and approver. Use it as a template whenever you need to stage temporary downgrades.
