import type { Page, Route } from "@playwright/test";
import type { Album, Photo, SharedAlbum, ShareSummary } from "../src/lib/types";

// 1x1 transparent PNG for every thumbnail/preview URL.
const PIXEL = Buffer.from(
  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=",
  "base64",
);

export function photo(overrides: Partial<Photo> & Pick<Photo, "photoId">): Photo {
  return {
    albumId: "a1",
    status: "Ready",
    caption: null,
    tags: [],
    width: 1600,
    height: 1200,
    createdAt: "2026-10-01T10:00:00Z",
    thumbnailUrl: `https://img.test/${overrides.photoId}-thumb.webp`,
    previewUrl: `https://img.test/${overrides.photoId}-preview.webp`,
    ...overrides,
  };
}

export interface MockState {
  albums: Album[];
  photos: Photo[];
  shares: ShareSummary[];
  shared: Record<string, SharedAlbum>;
  /** Every mutating request the page sent, e.g. "DELETE /api/albums/a1/photos/p1". */
  mutations: string[];
  /** Requests no handler matched; tests assert this stays empty. */
  unmocked: string[];
}

/** Fakes the Photos API in memory. Requests the handlers below do not know are recorded in `unmocked`. */
export async function mockApi(page: Page, initial: Partial<MockState> = {}): Promise<MockState> {
  const state: MockState = { albums: [], photos: [], shares: [], shared: {}, mutations: [], unmocked: [], ...initial };

  await page.route(/https:\/\/img\.test\//, (route) => route.fulfill({ body: PIXEL, contentType: "image/png" }));

  await page.route(/\/api\//, async (route: Route) => {
    const request = route.request();
    const url = new URL(request.url());
    const method = request.method();
    const path = url.pathname;
    const json = (body: unknown, status = 200) => route.fulfill({ status, json: body });

    if (method === "OPTIONS") return route.fulfill({ status: 204 });
    if (method !== "GET") state.mutations.push(`${method} ${path}`);

    if (method === "GET" && path === "/api/albums") return json(state.albums);

    if (method === "POST" && path === "/api/albums") {
      const { name } = request.postDataJSON() as { name: string };
      const album: Album = {
        albumId: `a${state.albums.length + 1}`,
        name,
        photoCount: 0,
        createdAt: "",
        coverUrl: null,
      };
      state.albums.push(album);
      return json(album, 201);
    }

    let match = path.match(/^\/api\/albums\/([^/]+)$/);
    if (match && method === "GET") {
      const album = state.albums.find((a) => a.albumId === match![1]);
      return album ? json(album) : json({}, 404);
    }

    match = path.match(/^\/api\/albums\/([^/]+)\/photos$/);
    if (match && method === "GET") {
      return json({ items: state.photos.filter((p) => p.albumId === match![1]), nextCursor: null });
    }

    match = path.match(/^\/api\/albums\/([^/]+)\/photos\/([^/]+)$/);
    if (match && method === "DELETE") {
      state.photos = state.photos.filter((p) => p.photoId !== match![2]);
      return route.fulfill({ status: 204 });
    }

    match = path.match(/^\/api\/albums\/([^/]+)\/shares$/);
    if (match && method === "GET") return json(state.shares);

    if (method === "GET" && path === "/api/tags") {
      const counts = new Map<string, number>();
      for (const p of state.photos) for (const tag of p.tags) counts.set(tag, (counts.get(tag) ?? 0) + 1);
      return json([...counts].map(([tag, count]) => ({ tag, count })));
    }

    if (method === "GET" && path === "/api/photos") {
      const tag = url.searchParams.get("tag")?.toLowerCase() ?? "";
      const items = state.photos.filter((p) => p.tags.some((t) => t.toLowerCase() === tag));
      return json({ items, nextCursor: null });
    }

    match = path.match(/^\/api\/shared\/([^/]+)$/);
    if (match && method === "GET") {
      const shared = state.shared[match[1]];
      return shared ? json(shared) : json({}, 404);
    }

    state.unmocked.push(`${method} ${path}`);
    return json({ error: "Not mocked" }, 501);
  });

  return state;
}
