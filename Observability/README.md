# Observability

This project ships logs in two places by default (see `WebApi/appsettings.json` → `Serilog:WriteTo`):

- **File** — always on, rolling daily, for local/on-box debugging.
- **Elasticsearch** — via `Elastic.Serilog.Sinks`, for centralized search and Grafana dashboards.

## Before deploying to a real environment

1. Replace `Serilog:WriteTo:Elasticsearch:Args:nodes` in `appsettings.json` (or override via
   environment variables / a secrets manager) with your real cluster address.
2. Every log line is enriched with `CorrelationId` (see `WebApi/Middleware/CorrelationIdMiddleware.cs`)
   and every API error response includes the same value as `traceId` — use it to jump from
   "a user reported an error" straight to the exact log lines in Kibana/Grafana.
3. If the Elasticsearch sink can't reach the cluster, it does **not** fail silently: enable
   visibility comes from `Serilog.Debugging.SelfLog` (wired in `Program.cs`), which writes sink
   errors to stderr. Watch for `[Serilog SelfLog]` lines in your container/host logs — if you see
   none of your application logs reaching Elasticsearch, check there first before assuming
   everything is fine.

## Grafana

`grafana/projectbase-api-overview.json` is a minimal starter dashboard (log volume by level, error
count, recent error log stream) against an Elasticsearch datasource pointed at the
`projectbase-logs-*` index pattern configured in `indexFormat`. Import it into Grafana, point the
`Elasticsearch` datasource variable at your cluster, and extend it with panels specific to your
project (request latency by endpoint, business-error-code breakdown by `ErrorCode`, etc.) once you
have real traffic to chart.
