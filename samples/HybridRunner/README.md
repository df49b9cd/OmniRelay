# Hybrid Batch + Realtime Runner

This sample shows how OmniRelay can coordinate asynchronous batch jobs and realtime dashboards within one dispatcher. Background workers enqueue jobs through an oneway procedure (`batch::enqueue`), while dashboards consume server streams (`dashboard::stream`) to watch progress in realtime. Middleware applies deadlines and retry budgets so fire-and-forget workloads remain controlled.

## Run

```bash
dotnet run --project samples/HybridRunner
```

- HTTP inbound: `http://127.0.0.1:7220`
- Enqueue jobs by POSTing JSON to `/yarpc/v1/batch::enqueue` with `{"batchId":"abc","customer":"north","tasks":5}`
- Stream progress: `omnirelay request --transport http --url http://127.0.0.1:7220/yarpc/v1 --service samples.hybrid-runner --procedure dashboard::stream --encoding application/json`

## Key concepts

- **Oneway batch ingestion:** Jobs are enqueued via oneway RPCs that carry deadlines and retry budgets.
- **Realtime dashboards:** Server-streaming handler pushes `ProgressUpdate` messages as background workers advance jobs.
- **Shared middleware:** Deadline + retry middleware illustrate how to apply consistent policies across asynchronous and realtime workloads.
- **Background worker:** `BatchWorker` reads jobs from a channel and publishes progress updates, simulating a batch pipeline.
