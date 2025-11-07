# Chaos & Failover Lab

Exercise OmniRelay’s peer choosers, circuit breakers, and retry policies without touching production. This lab spins up two unstable backends and routes requests through a dispatcher that applies retries, deadlines, and random chaos, so reliability engineers can watch failover behavior in a tight loop.

## Run

```bash
dotnet run --project samples/ChaosFailover.Lab
```

- HTTP inbound: `http://127.0.0.1:7230/yarpc/v1/chaos::ping`
- Use `omnirelay request` to send traffic or rely on the built-in traffic generator (runs automatically)

## What to watch

- `backend-a` succeeds ~90% of the time, `backend-b` succeeds ~50%—the traffic generator chooses secondary 20% of the time to illustrate failover.
- The dispatcher applies retry and deadline middleware to mimic production policy.
- Inspect `/omnirelay/introspect` (via Observability sample or CLI) to observe peer health as failures occur.

## Suggested workflow

1. Start the lab and run `omnirelay request --transport http --url http://127.0.0.1:7230/yarpc/v1 --service samples.chaos-lab --procedure chaos::ping --encoding application/json --body '{"useSecondary":false}'`.
2. Use the CLI script (pending addition) to flip between primary/secondary and capture fail counts.
3. Tail the console to see failures, retries, and final responses.
