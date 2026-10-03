# 0005 — Static-export Next.js frontend with TanStack Query

**Status:** Accepted

## Context

All data is per user and comes from the API; there is no SEO-relevant content and nothing to render on a
server. The frontend should be hostable on S3 + CloudFront with no servers to run.

## Decision

- **Next.js 16** App Router with `output: "export"` — plain HTML and JS files. Dynamic pages use query strings
  (`/album?id=…`, `/shared?code=…`) instead of dynamic route segments, which a static export cannot pre-render.
- **TanStack Query 5** owns server state: caching, `useInfiniteQuery` pagination, conditional polling and
  targeted invalidation after mutations.
- **React 19 with the React Compiler**, Tailwind CSS 4, native `<dialog>` for modals.

## Consequences

- Hosting is a bucket and a CDN; no Node.js runtime in production.
- No `next/image` optimization (it needs a server) — the backend already produces WebP variants.
- E2E tests serve the real `out/` folder and mock the API, so they test exactly what would be deployed.
