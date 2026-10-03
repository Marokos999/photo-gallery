# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] — 2026-10-03

First complete release: a serverless photo gallery on AWS Lambda (.NET 10) with a Next.js 16 static frontend,
runnable end to end with LocalStack.

### Features

- Albums: create, rename, delete (removes every photo, tag pointer, share link and S3 object).
- Direct-to-S3 uploads with presigned POST forms: drag and drop, per-file progress, three parallel uploads;
  S3 enforces the 25 MB limit and the content type.
- Automatic processing: auto-orient, EXIF/GPS stripping, 400 px and 1200 px WebP variants; images over 50 MP
  and undecodable files are marked `Failed`.
- Masonry grid with lightbox, infinite scroll (cursor pagination), live *Processing…* status.
- Captions and tags on upload or later; tag search across albums with a tag cloud.
- Expiring public share links (1–30 days) with list, copy and revoke.
- Accessible confirm dialogs, error and not-found pages.

### Architecture

- Single-table DynamoDB with transactional tag pointer items, GSI1 only for share links, TTL for abandoned
  uploads and a DynamoDB Streams consumer that removes their S3 objects.
- Photos API as an ASP.NET Core Minimal API hosted in Lambda; upload and processing as plain Lambda handlers.
- Powertools for AWS Lambda: structured JSON logs, CloudWatch EMF metrics, X-Ray tracing.
- System.Text.Json source generators for Lambda events and API contracts.
- AWS SAM template with a Cognito user pool, JWT authorizer (public share and health routes) and an
  `AllowedOrigin` parameter for CORS.
- Architecture Decision Records in `docs/adr`.

### Quality

- 122 .NET tests (unit, `WebApplicationFactory` API tests, LocalStack integration tests) with code coverage.
- Vitest + Testing Library unit tests and Playwright E2E smoke tests against the static export.
- GitHub Actions CI with coverage summary, CodeQL analysis and Dependabot updates.

[1.0.0]: https://github.com/Marokos999/photo-gallery/releases/tag/v1.0.0
