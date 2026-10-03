# 0003 — Single-table DynamoDB with tag pointer items

**Status:** Accepted

## Context

Access patterns are known up front and all of them are per user: list albums, page through an album's photos
newest first, list a user's tags with counts, find photos by tag across albums, resolve a public share code,
list an album's share links. Relational joins are not needed; predictable single-digit-ms reads are.

## Decision

One table, `PK`/`SK`, with IDs as **UUIDv7** so sort keys are creation-ordered:

| Item | PK | SK |
| --- | --- | --- |
| Album | `USER#{u}` | `ALBUM#{a}` |
| Photo | `USER#{u}` | `PHOTO#{a}#{p}` |
| Tag pointer | `USER#{u}` | `TAG#{tag}#{p}` |
| Share | `SHARE#{code}` | `SHARE` |

- **Tag pointers** are written in the same `TransactWriteItems` as the photo (create, edit, delete), so search
  by tag is one `Query` on `begins_with(SK, "TAG#beach#")` and can never drift from the photo.
- **GSI1** indexes only share items (`ALBUM#{a}` / `SHARE#{code}`) to list an album's links. Photos and albums
  carry no GSI keys, so the bulk of writes pays for no index.
- Pagination uses an opaque Base64Url cursor over `LastEvaluatedKey`.

## Alternatives considered

- **GSI on tags** — DynamoDB cannot index list elements; a GSI would need one item per tag anyway.
- **Scan + filter** — cost grows with the whole table, not with the result.

## Consequences

- Every screen is a single `Query` or `GetItem`.
- New access patterns need deliberate key design (and possibly a backfill), unlike ad-hoc SQL.
- Tag counts are computed from pointer items; fine for a personal library, would need counters at large scale.
