# 0001 — Serverless on AWS Lambda, defined with SAM

**Status:** Accepted

## Context

A photo gallery has spiky, mostly idle traffic: bursts of uploads, then long quiet periods. The work splits
into three very different shapes — short API calls, CPU-heavy image resizing, and background cleanup — and
the project should cost nothing when nobody uses it. It also has to run end to end on a laptop.

## Decision

- Four .NET 10 Lambda functions on **arm64**: `UploadFunction` (issues upload forms), `ProcessingFunction`
  (S3-triggered resizing), `ExpiredUploadCleanupFunction` (DynamoDB Streams) and `PhotosFunction`
  (an ASP.NET Core Minimal API hosted in Lambda).
- Infrastructure as code with **AWS SAM** (`template.yaml`): one HTTP API, one bucket, one table, one user pool.
- Locally, **LocalStack** provides S3, DynamoDB and SQS; `sam local` runs the upload Lambda and a small console
  tool feeds S3 notifications from SQS into the real `ProcessingFunction`.

## Consequences

- Pay-per-use, scale to zero, and each function gets its own memory and timeout (2 GB for resizing, 256 MB for cleanup).
- The Photos API stays a normal ASP.NET Core app: routing, `TypedResults`, ProblemDetails and
  `WebApplicationFactory` tests all work unchanged; only the host differs.
- Cold starts exist. Mitigated with ReadyToRun, arm64 and source-generated JSON ([0007](0007-powertools-and-source-generated-json.md)).
- `sam local` cannot deliver S3 events, hence the local processor shim.
