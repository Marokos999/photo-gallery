// Minimal static file server for the Next.js export in out/, used by Playwright.
// Mirrors how S3/CloudFront serve the site: /album -> album.html, unknown paths -> 404.html.
import { createReadStream, existsSync, statSync } from "node:fs";
import { createServer } from "node:http";
import { extname, join, normalize } from "node:path";

const root = join(import.meta.dirname, "..", "out");
const port = Number(process.env.PORT ?? 3100);
const types = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript",
  ".css": "text/css",
  ".txt": "text/plain; charset=utf-8",
  ".json": "application/json",
  ".svg": "image/svg+xml",
  ".ico": "image/x-icon",
  ".woff2": "font/woff2",
};

function resolve(pathname) {
  const base = normalize(join(root, decodeURIComponent(pathname)));
  if (!base.startsWith(root)) return null;
  for (const candidate of [base, `${base}.html`, join(base, "index.html")]) {
    if (existsSync(candidate) && statSync(candidate).isFile()) return candidate;
  }
  return null;
}

createServer((request, response) => {
  const { pathname } = new URL(request.url, "http://localhost");
  const file = resolve(pathname);
  const status = file ? 200 : 404;
  const path = file ?? join(root, "404.html");
  response.writeHead(status, { "Content-Type": types[extname(path)] ?? "application/octet-stream" });
  createReadStream(path).pipe(response);
}).listen(port, () => console.log(`Serving out/ on http://localhost:${port}`));
