import { config } from "./config";
import type { Album, Photo, Share, SharedAlbum, UploadRequest, UploadTicket } from "./types";

export class ApiError extends Error {
  readonly status: number;
  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

function authHeaders(): Record<string, string>{
return config.debugUserId ? { "x-debug-user-id": config.debugUserId } : {};
};

async function request<T>(baseUrl: string, path: string, init: RequestInit = {}, authenticated = true): Promise<T> {

  const headers = new Headers(init.headers);

  if(init.body !== undefined) headers.set("Content-Type", "application/json");

  if(authenticated){
    for(const [name, value] of Object.entries(authHeaders())) headers.set(name, value);
  }

  const response = await fetch(`${baseUrl}${path}`, { ...init, headers });
  if(!response.ok){
    throw new ApiError(response.status, `${init.method ?? "GET"} ${path} failed with ${response.status}`);
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const api = {
  listAlbums: () => request<Album[]>(config.apiUrl, "/api/albums"),

  createAlbum: (name: string) =>
    request<Album>(config.apiUrl, "/api/albums", { method: "POST", body: JSON.stringify({ name }) }),

  listPhotos: (albumId: string) => request<Photo[]>(config.apiUrl, `/api/albums/${albumId}/photos`),

  deletePhoto: (albumId: string, photoId: string) =>
    request<void>(config.apiUrl, `/api/albums/${albumId}/photos/${photoId}`, {method: "DELETE"}),

  createShare: (albumId: string, expiresInDays: number) =>
    request<Share>(config.apiUrl, `/api/albums/${albumId}/share`,{
      method: "POST",
      body: JSON.stringify({expiresInDays}),
    }),

  getShareAlbum: (code: string) =>
    request<SharedAlbum>(config.apiUrl, `/api/shared/${encodeURIComponent(code)}`, {}, false),

  requestUpload: (input: UploadRequest) =>
    request<UploadTicket>(config.uploadUrl, "/api/photos/upload-url", {
      method: "POST",
      body: JSON.stringify(input),
    }),

};
