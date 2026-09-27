import { config } from "./config";
import type { Album, Photo, PhotoDetails, Share, SharedAlbum, UploadRequest, UploadTicket } from "./types";

export class ApiError extends Error {
  readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

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

  listPhotos: (albumId: string) => request<Photo[]>(config.apiUrl, `/api/albums/${albumId}/photos`),

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
 * PUTs a file straight to S3 using a presigned URL.
 * Uses XMLHttpRequest because fetch() still has no upload progress events.
 */
export function uploadFile(uploadUrl: string, file: File, onProgress?: (fraction: number) => void): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open("PUT", uploadUrl);
    xhr.setRequestHeader("Content-Type", file.type);
    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) onProgress?.(event.loaded / event.total);
    };
    xhr.onload = () =>
      xhr.status >= 200 && xhr.status < 300
        ? resolve()
        : reject(new ApiError(xhr.status, `Upload failed with ${xhr.status}`));
    xhr.onerror = () => reject(new ApiError(0, "Upload failed: network error"));
    xhr.send(file);
  });
}
