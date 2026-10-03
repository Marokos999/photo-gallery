export type PhotoStatus = "Pending" | "Ready" | "Failed";

export interface Album {
  albumId: string;
  name: string;
  photoCount: number;
  createdAt: string;
  coverUrl: string | null;
}

export interface Photo {
  photoId: string;
  albumId: string;
  status: PhotoStatus;
  caption: string | null;
  tags: string[];
  width: number | null;
  height: number | null;
  createdAt: string;
  thumbnailUrl: string | null;
  previewUrl: string | null;
}

export interface PhotoPage {
  items: Photo[];
  /** Pass back as `cursor` to get the next (older) page; null on the last page. */
  nextCursor: string | null;
}

export interface Share {
  code: string;
  expiresAt: string;
}

export interface SharedPhoto {
  photoId: string;
  caption: string | null;
  width: number | null;
  height: number | null;
  thumbnailUrl: string | null;
  previewUrl: string | null;
}

export interface SharedAlbum {
  name: string;
  expiresAt: string | null;
  photos: SharedPhoto[];
}

export interface UploadRequest {
  fileName: string;
  contentType: string;
  albumId: string;
  caption?: string;
  tags?: string[];
}

export interface UploadTicket {
  photoId: string;
  key: string;
  uploadUrl: string;
  /** Presigned POST policy fields; must be sent before the file. */
  uploadFields: Record<string, string>;
  expiresAt: string;
}
export interface PhotoDetails {
  caption: string | null;
  tags: string[];
}

export interface TagCount {
  tag: string;
  count: number;
}

export interface ShareSummary {
  code: string;
  createdAt: string;
  expiresAt: string | null;
  isExpired: boolean;
}
