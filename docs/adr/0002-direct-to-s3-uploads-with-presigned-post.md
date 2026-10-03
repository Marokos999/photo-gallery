# 0002 — Direct-to-S3 uploads with presigned POST

**Status:** Accepted

## Context

Photos can be up to 25 MB. Sending bytes through API Gateway and Lambda hits the 10 MB API Gateway payload
limit and the 6 MB Lambda payload limit, adds latency and pays Lambda time for copying bytes. The server must
still decide *who* may upload *what*, *where* and *how big*.

## Decision

The browser asks `UploadFunction` for an upload form. The function validates the request, writes a
`Pending` photo record (with a 24 h TTL) and returns a **presigned POST** policy that pins:

- the exact object key (`originals/{user}/{album}/{photo}/{file}`),
- the exact `Content-Type`,
- `content-length-range` 1 B – 25 MB,
- a 5 minute expiry.

The browser then posts the file straight to S3, with progress events via `XMLHttpRequest`.

## Alternatives considered

- **Presigned PUT** — simpler, but cannot limit the object size; a client could upload gigabytes.
- **Upload through the API** — blocked by the payload limits above.

## Consequences

- Lambda never touches image bytes on upload; S3 itself rejects oversized or mistyped files.
- Uploads that never happen leave only a `Pending` record, which TTL removes ([0004](0004-asynchronous-image-processing.md)).
- The S3 bucket needs CORS for the frontend origin (`AllowedOrigin` template parameter).
