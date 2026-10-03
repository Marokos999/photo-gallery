import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { api, ApiError } from "./api";

vi.mock("./config", () => ({
  config: { apiUrl: "https://api.test", uploadUrl: "https://upload.test", debugUserId: "dev-user" },
}));

const fetchMock = vi.fn<typeof fetch>();

function lastRequest() {
  const [url, init] = fetchMock.mock.lastCall!;
  return { url: String(url), headers: new Headers(init?.headers), method: init?.method ?? "GET" };
}

beforeEach(() => {
  vi.stubGlobal("fetch", fetchMock);
  fetchMock.mockResolvedValue(Response.json([]));
});

afterEach(() => {
  vi.unstubAllGlobals();
  fetchMock.mockReset();
});

describe("api", () => {
  it("sends the debug user header on owner requests", async () => {
    await api.listTags();

    const request = lastRequest();
    expect(request.url).toBe("https://api.test/api/tags");
    expect(request.headers.get("x-debug-user-id")).toBe("dev-user");
  });

  it("does not identify the user on public share requests", async () => {
    fetchMock.mockResolvedValue(Response.json({ name: "Trip", expiresAt: null, photos: [] }));

    await api.getSharedAlbum("abc");

    expect(lastRequest().headers.has("x-debug-user-id")).toBe(false);
  });

  it("encodes the tag and cursor in search requests", async () => {
    fetchMock.mockResolvedValue(Response.json({ items: [], nextCursor: null }));

    await api.searchPhotos("road trip", "c/1");

    expect(lastRequest().url).toBe("https://api.test/api/photos?tag=road%20trip&limit=60&cursor=c%2F1");
  });

  it("sends JSON bodies with a content type", async () => {
    fetchMock.mockResolvedValue(Response.json({ albumId: "a1", name: "Trip", photoCount: 0 }, { status: 201 }));

    await api.createAlbum("Trip");

    const [, init] = fetchMock.mock.lastCall!;
    expect(lastRequest().method).toBe("POST");
    expect(lastRequest().headers.get("Content-Type")).toBe("application/json");
    expect(init?.body).toBe(JSON.stringify({ name: "Trip" }));
  });

  it("returns undefined for 204 responses", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }));

    await expect(api.revokeShare("abc")).resolves.toBeUndefined();
  });

  it("throws ApiError with the status code on failure", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 404 }));

    const error = await api.getAlbum("missing").catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(404);
  });
});
