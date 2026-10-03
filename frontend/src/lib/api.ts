import { config } from "./config";
import type { Album, PhotoDetails, PhotoPage, Share, SharedAlbum, UploadRequest, UploadTicket } from "./types";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export const PHOTO_PAGE_SIZE = 60;

function authHeaders(): Record<string, string> {
  return config.debugUserId ? { "x-debug-user-id": config.debugUserId } : {};
}

async function request<T>(baseUrl: string, path: string, init: RequestInit = {}, authenticated = true): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body !== undefined) headers.set("Content-Type", "application/json");
  if (authenticated) {
    for (const [name, value] of Object.entries(authHeaders())) headers.set(name, value);
  }

  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  if (!response.ok) {
    throw new ApiError(response.status, `${init.method ?? "GET"} ${path} failed with ${response.status}`);
  }

  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const api = {
  listAlbums: () => request<Album[]>(config.apiUrl, "/api/albums"),

  createAlbum: (name: string) =>
    request<Album>(config.apiUrl, "/api/albums", { method: "POST", body: JSON.stringify({ name }) }),

  getAlbum: (albumId: string) => request<Album>(config.apiUrl, `/api/albums/${albumId}`),

  renameAlbum: (albumId: string, name: string) =>
    request<void>(config.apiUrl, `/api/albums/${albumId}`, { method: "PATCH", body: JSON.stringify({ name }) }),

  deleteAlbum: (albumId: string) => request<void>(config.apiUrl, `/api/albums/${albumId}`, { method: "DELETE" }),

  listPhotos: (albumId: string, cursor?: string) =>
    request<PhotoPage>(
      config.apiUrl,
      `/api/albums/${albumId}/photos?limit=${PHOTO_PAGE_SIZE}` +
        (cursor ? `&cursor=${encodeURIComponent(cursor)}` : ""),
    ),

  updatePhoto: (albumId: string, photoId: string, details: PhotoDetails) =>
    request<void>(config.apiUrl, `/api/albums/${albumId}/photos/${photoId}`, {
      method: "PATCH",
      body: JSON.stringify(details),
    }),

  deletePhoto: (albumId: string, photoId: string) =>
    request<void>(config.apiUrl, `/api/albums/${albumId}/photos/${photoId}`, { method: "DELETE" }),

  createShare: (albumId: string, expiresInDays: number) =>
    request<Share>(config.apiUrl, `/api/albums/${albumId}/share`, {
      method: "POST",
      body: JSON.stringify({ expiresInDays }),
    }),

  getSharedAlbum: (code: string) =>
    request<SharedAlbum>(config.apiUrl, `/api/shared/${encodeURIComponent(code)}`, {}, false),

  requestUpload: (input: UploadRequest) =>
    request<UploadTicket>(config.uploadUrl, "/api/photos/upload-url", {
      method: "POST",
      body: JSON.stringify(input),
    }),
};

/**
 * Uploads a file straight to S3 with a presigned POST form. S3 enforces the policy (size, content type).
 * Uses XMLHttpRequest because fetch() still has no upload progress events.
 */
export function uploadFile(
  ticket: Pick<UploadTicket, "uploadUrl" | "uploadFields">,
  file: File,
  onProgress?: (fraction: number) => void,
): Promise<void> {
  const form = new FormData();
  for (const [name, value] of Object.entries(ticket.uploadFields)) form.append(name, value);
  form.append("file", file); // S3 ignores every field after "file", so it must be last.

  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("POST", ticket.uploadUrl);
    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(event.loaded / event.total);
    };
    xhr.onload = () =>
      xhr.status >= 200 && xhr.status < 300
        ? resolve()
        : reject(new ApiError(xhr.status, `Upload failed with ${xhr.status}`));
    xhr.onerror = () => reject(new ApiError(0, "Upload failed: network error"));
    xhr.send(form);
  });
}
