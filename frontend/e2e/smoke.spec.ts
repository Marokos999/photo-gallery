import { expect, test } from "@playwright/test";
import { mockApi, photo } from "./mock-api";

const album = { albumId: "a1", name: "Summer trip", photoCount: 2, createdAt: "2026-10-01T10:00:00Z", coverUrl: null };

test("lists albums and creates a new one", async ({ page }) => {
  const api = await mockApi(page, { albums: [album] });

  await page.goto("/");
  await expect(page.getByRole("link", { name: /Summer trip/ })).toBeVisible();

  await page.getByPlaceholder("New album name").fill("Winter");
  await page.getByRole("button", { name: "Create album" }).click();

  await expect(page.getByRole("link", { name: /Winter/ })).toBeVisible();
  expect(api.mutations).toContain("POST /api/albums");
  expect(api.unmocked).toEqual([]);
});

test("opens an album, follows a tag to search results", async ({ page }) => {
  const api = await mockApi(page, {
    albums: [album],
    photos: [
      photo({ photoId: "p1", caption: "Beach day", tags: ["beach"] }),
      photo({ photoId: "p2", caption: "Mountains", tags: ["hiking"] }),
    ],
  });

  await page.goto("/");
  await page.getByRole("link", { name: /Summer trip/ }).click();

  await expect(page.getByRole("heading", { name: "Summer trip" })).toBeVisible();
  await expect(page.getByAltText("Beach day")).toBeVisible();

  await page.getByRole("link", { name: "#beach" }).click();

  await expect(page).toHaveURL(/\/search\?tag=beach/);
  await expect(page.getByRole("heading", { name: "#beach" })).toBeVisible();
  await expect(page.getByAltText("Beach day")).toBeVisible();
  await expect(page.getByAltText("Mountains")).toHaveCount(0);
  expect(api.unmocked).toEqual([]);
});

test("deletes a photo only after confirmation", async ({ page }) => {
  const api = await mockApi(page, { albums: [album], photos: [photo({ photoId: "p1", caption: "Beach day" })] });

  await page.goto("/album?id=a1");
  await page.getByAltText("Beach day").hover();
  await page.getByRole("button", { name: "Delete photo" }).click();

  const dialog = page.getByRole("dialog", { name: "Delete photo?" });
  await dialog.getByRole("button", { name: "Cancel" }).click();
  expect(api.mutations).toEqual([]);

  await page.getByRole("button", { name: "Delete photo" }).click();
  await dialog.getByRole("button", { name: "Delete" }).click();

  await expect(page.getByText("No photos in this album yet.")).toBeVisible();
  expect(api.mutations).toEqual(["DELETE /api/albums/a1/photos/p1"]);
});

test("shows a shared album without owner navigation", async ({ page }) => {
  await mockApi(page, {
    shared: {
      xyz: {
        name: "Summer trip",
        expiresAt: "2030-01-01T00:00:00Z",
        photos: [
          {
            photoId: "p1",
            caption: "Beach day",
            width: 1600,
            height: 1200,
            thumbnailUrl: "https://img.test/p1.webp",
            previewUrl: "https://img.test/p1-preview.webp",
          },
        ],
      },
    },
  });

  await page.goto("/shared?code=xyz");

  await expect(page.getByRole("heading", { name: "Summer trip" })).toBeVisible();
  await expect(page.getByAltText("Beach day")).toBeVisible();
  await expect(page.getByRole("link", { name: "Search" })).toHaveCount(0);
});

test("shows an expired share link as unavailable", async ({ page }) => {
  await mockApi(page);

  await page.goto("/shared?code=gone");

  await expect(page.getByRole("heading", { name: "Link not available" })).toBeVisible();
});
