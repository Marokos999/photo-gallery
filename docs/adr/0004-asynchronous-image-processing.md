# 0004 — Asynchronous image processing with explicit failure and TTL cleanup

**Status:** Accepted

## Context

Resizing a 25 MB photo takes seconds and a lot of memory. The upload request must return immediately, the
UI must know when a photo is ready, and broken uploads (corrupt files, decompression bombs, abandoned
uploads) must neither leave the UI waiting forever nor leave orphaned objects in S3.

## Decision

- An S3 `ObjectCreated` event on `originals/` triggers `ProcessingFunction`: auto-orient, strip EXIF/GPS,
  produce 400 px and 1200 px **WebP** variants, then mark the photo `Ready` with a conditional update.
- Images over **50 megapixels** are rejected after `Identify` (before decoding), and undecodable files are
  caught; both mark the photo **`Failed`** — the same bytes would fail on retry.
- A `Pending` record carries a 24 h TTL. When TTL deletes it, a **DynamoDB Streams** consumer
  (`ExpiredUploadCleanupFunction`, filtered to `REMOVE` events made by the DynamoDB service) deletes
  whatever reached S3 for it.
- The frontend polls only while some visible photo is `Pending`.

## Consequences

- Upload latency is independent of image size; processing scales per object.
- Three explicit outcomes (`Ready`, `Failed`, expired) — nothing waits forever.
- Idempotent: a duplicate S3 event finds the photo no longer `Pending` and skips it.
- Polling instead of push (WebSockets) keeps the stack small; acceptable for processing that takes seconds.
