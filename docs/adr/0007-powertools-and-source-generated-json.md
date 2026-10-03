# 0007 — Powertools observability and source-generated JSON

**Status:** Accepted

## Context

With four functions and asynchronous flows, a failed upload has to be traceable from the upload request to
processing. Custom metrics (processed, failed, cleaned up) show whether the pipeline is healthy. Lambda cold
starts are sensitive to reflection-heavy startup work such as `System.Text.Json` metadata discovery.

## Decision

- **Powertools for AWS Lambda (.NET)**: `[Logging]` for structured JSON logs with request id, cold start and
  correlation id; `[Metrics]` writing CloudWatch **EMF** (no `PutMetricData` calls); `[Tracing]` plus X-Ray
  instrumentation of the AWS SDK. Tracing is switched off under LocalStack and `sam local`.
- The Photos API uses the Powertools logger provider when running in Lambda and plain console logging locally.
- **System.Text.Json source generators** for Lambda event envelopes (`SourceGeneratorLambdaJsonSerializer`)
  and API contracts (first in the resolver chain).

## Consequences

- Logs are queryable in CloudWatch Logs Insights; metrics appear without extra infrastructure.
- Less reflection at cold start; serialization mistakes are caught by tests that feed AWS-shaped events.
- Every new request or response type must be added to its `JsonSerializerContext`. Tests catch omissions for
  Lambda events; the API falls back to reflection for anything missed.
